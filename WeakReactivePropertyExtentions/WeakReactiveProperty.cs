using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Reactive.Bindings.WeakExtentions
{
    /// <summary>
    ///     Two-way bindable IObserable&lt;T&gt;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WeakReactiveProperty<T> : IReactiveProperty<T>, IReadOnlyReactiveProperty<T>
    {
        /// <summary>PropertyChanged raise on ReactivePropertyScheduler</summary>
        public WeakReactiveProperty()
            : this(
                default(T), ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe
            )
        {
        }

        /// <summary>PropertyChanged raise on ReactivePropertyScheduler</summary>
        public WeakReactiveProperty(
            T initialValue = default(T),
            ReactivePropertyMode mode =
                ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe)
            : this(ReactivePropertyScheduler.Default, initialValue, mode)
        {
        }

        /// <summary>PropertyChanged raise on selected scheduler</summary>
        public WeakReactiveProperty(
            IScheduler raiseEventScheduler,
            T initialValue = default(T),
            ReactivePropertyMode mode =
                ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe)
        {
            RaiseEventScheduler = raiseEventScheduler;
            LatestValue = initialValue;

            IsRaiseLatestValueOnSubscribe = mode.HasFlag(ReactivePropertyMode.RaiseLatestValueOnSubscribe);
            IsDistinctUntilChanged = mode.HasFlag(ReactivePropertyMode.DistinctUntilChanged);

            SourceDisposable = Disposable.Empty;
            ErrorsTrigger =
                new Lazy<BehaviorSubject<IEnumerable>>(() => new BehaviorSubject<IEnumerable>(GetErrors(null)));
        }

        // ToReactiveProperty Only
        public WeakReactiveProperty(
            IObservable<T> source,
            T initialValue = default(T),
            ReactivePropertyMode mode =
                ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe)
            : this(source, ReactivePropertyScheduler.Default, initialValue, mode)
        {
        }

        public WeakReactiveProperty(
            IObservable<T> source,
            IScheduler raiseEventScheduler,
            T initialValue = default(T),
            ReactivePropertyMode mode =
                ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe)
            : this(raiseEventScheduler, initialValue, mode)
        {
            SourceDisposable = source.WeakSubscribe(new AnonymousObserver<T>(x => Value = x));
        }

        private T LatestValue { get; set; }
        private bool IsDisposed { get; set; }
        public IScheduler RaiseEventScheduler { get; }
        public bool IsDistinctUntilChanged { get; }
        public bool IsRaiseLatestValueOnSubscribe { get; }

        private Subject<T> Source { get; } = new Subject<T>();
        private IDisposable SourceDisposable { get; }
        private bool IsValueChanging { get; set; } = false;

        // for Validation
        private Subject<T> ValidationTrigger { get; } = new Subject<T>();
        private SerialDisposable ValidateNotifyErrorSubscription { get; } = new SerialDisposable();
        private Lazy<BehaviorSubject<IEnumerable>> ErrorsTrigger { get; }

        private Lazy<List<Func<IObservable<T>, IObservable<IEnumerable>>>> ValidatorStore { get; } =
            new Lazy<List<Func<IObservable<T>, IObservable<IEnumerable>>>>(
                () => new List<Func<IObservable<T>, IObservable<IEnumerable>>>());

        // INotifyDataErrorInfo
        private IEnumerable CurrentErrors { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Get latestValue or push(set) value.
        /// </summary>
        public T Value
        {
            get { return LatestValue; }
            set
            {
                if (LatestValue == null || value == null)
                {
                    // null case
                    if (IsDistinctUntilChanged && LatestValue == null && value == null)
                        return;

                    SetValue(value);
                    return;
                }

                if (IsDistinctUntilChanged && EqualityComparer<T>.Default.Equals(LatestValue, value))
                    return;

                SetValue(value);
            }
        }

        object IReactiveProperty.Value
        {
            get { return Value; }
            set { Value = (T) value; }
        }

        /// <summary>
        ///     Subscribe source.
        /// </summary>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (IsRaiseLatestValueOnSubscribe)
            {
                observer.OnNext(LatestValue);
                return Source.WeakSubscribe(observer);
            }
            return Source.WeakSubscribe(observer);
        }

        /// <summary>
        ///     Unsubcribe all subscription.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;
            Source.OnCompleted();
            Source.Dispose();
            ValidationTrigger.Dispose();
            SourceDisposable.Dispose();
            ValidateNotifyErrorSubscription.Dispose();
            if (ErrorsTrigger.IsValueCreated)
            {
                ErrorsTrigger.Value.OnCompleted();
                ErrorsTrigger.Value.Dispose();
            }
        }

        // Validations

        /// <summary>
        ///     <para>Checked validation, raised value. If success return value is null.</para>
        /// </summary>
        public IObservable<IEnumerable> ObserveErrorChanged => ErrorsTrigger.Value.AsObservable();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>Get INotifyDataErrorInfo's error store</summary>
        public IEnumerable GetErrors(string propertyName) => CurrentErrors;

        /// <summary>Get INotifyDataErrorInfo's error store</summary>
        public bool HasErrors => CurrentErrors != null;

        /// <summary>
        ///     Observe HasErrors value.
        /// </summary>
        public IObservable<bool> ObserveHasErrors => ObserveErrorChanged.Select(_ => HasErrors);

        object IReadOnlyReactiveProperty.Value => Value;

        public override string ToString() =>
            LatestValue == null
                ? "null"
                : "{" + LatestValue.GetType().Name + ":" + LatestValue + "}";

        /// <summary>
        ///     <para>Set INotifyDataErrorInfo's asynchronous validation, return value is self.</para>
        /// </summary>
        /// <param name="validator">If success return IO&lt;null&gt;, failure return IO&lt;IEnumerable&gt;(Errors).</param>
        /// <returns>Self.</returns>
        public WeakReactiveProperty<T> SetValidateNotifyError(Func<IObservable<T>, IObservable<IEnumerable>> validator)
        {
            ValidatorStore.Value.Add(validator); //--- cache validation functions
            var validators = ValidatorStore.Value
                .Select(x => x(ValidationTrigger.StartWith(LatestValue)))
                .ToArray(); //--- use copy
            ValidateNotifyErrorSubscription.Disposable
                = Observable.CombineLatest(validators)
                    .Select(xs =>
                    {
                        if (xs.Count == 0) return null;
                        if (xs.All(x => x == null)) return null;

                        var strings = xs
                            .OfType<string>()
                            .Where(x => x != null);
                        var others = xs
                            .Where(x => !(x is string))
                            .Where(x => x != null)
                            .SelectMany(x => x.Cast<object>());
                        return strings.Concat(others);
                    })
                    .WeakSubscribe(new AnonymousObserver<IEnumerable<object>>(x =>
                    {
                        CurrentErrors = x;
                        var handler = ErrorsChanged;
                        if (handler != null)
                            RaiseEventScheduler.Schedule(
                                () => handler(this, SingletonDataErrorsChangedEventArgs.Value));
                        ErrorsTrigger.Value.OnNext(x);
                    }));
            return this;
        }

        /// <summary>
        ///     <para>Set INotifyDataErrorInfo's asynchronous validation, return value is self.</para>
        /// </summary>
        /// <param name="validator">If success return IO&lt;null&gt;, failure return IO&lt;IEnumerable&gt;(Errors).</param>
        /// <returns>Self.</returns>
        public WeakReactiveProperty<T> SetValidateNotifyError(Func<IObservable<T>, IObservable<string>> validator) =>
            SetValidateNotifyError(xs => validator(xs).Cast<IEnumerable>());

        /// <summary>
        ///     Set INotifyDataErrorInfo's asynchronous validation.
        /// </summary>
        /// <param name="validator">Validation logic</param>
        /// <returns>Self.</returns>
        public WeakReactiveProperty<T> SetValidateNotifyError(Func<T, Task<IEnumerable>> validator) =>
            SetValidateNotifyError(xs => xs.SelectMany(x => validator(x)));

        /// <summary>
        ///     Set INotifyDataErrorInfo's asynchronous validation.
        /// </summary>
        /// <param name="validator">Validation logic</param>
        /// <returns>Self.</returns>
        public WeakReactiveProperty<T> SetValidateNotifyError(Func<T, Task<string>> validator) =>
            SetValidateNotifyError(xs => xs.SelectMany(x => validator(x)));

        /// <summary>
        ///     Set INofityDataErrorInfo validation.
        /// </summary>
        /// <param name="validator">Validation logic</param>
        /// <returns>Self.</returns>
        public WeakReactiveProperty<T> SetValidateNotifyError(Func<T, IEnumerable> validator) =>
            SetValidateNotifyError(xs => xs.Select(x => validator(x)));

        /// <summary>
        ///     Set INofityDataErrorInfo validation.
        /// </summary>
        /// <param name="validator">Validation logic</param>
        /// <returns>Self.</returns>
        public WeakReactiveProperty<T> SetValidateNotifyError(Func<T, string> validator) =>
            SetValidateNotifyError(xs => xs.Select(x => validator(x)));

        /// <summary>
        ///     Invoke validation process.
        /// </summary>
        public void ForceValidate() => ValidationTrigger.OnNext(LatestValue);

        /// <summary>
        ///     Invoke OnNext.
        /// </summary>
        public void ForceNotify() => SetValue(LatestValue);

        private void SetValue(T value)
        {
            LatestValue = value;
            ValidationTrigger.OnNext(value);
            Source.OnNext(value);
            RaiseEventScheduler.Schedule(
                () => PropertyChanged?.Invoke(this, SingletonPropertyChangedEventArgs.Value));
        }
    }

    public static class WeakReactiveProperty
    {
        /// <summary>
        ///     <para>Convert to two-way bindable IObservable&lt;T&gt;</para>
        ///     <para>PropertyChanged raise on ReactivePropertyScheduler</para>
        /// </summary>
        public static WeakReactiveProperty<T> ToWeakReactiveProperty<T>(this IObservable<T> source,
            T initialValue = default(T),
            ReactivePropertyMode mode =
                ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe) =>
            new WeakReactiveProperty<T>(source, initialValue, mode);

        /// <summary>
        ///     <para>Convert to two-way bindable IObservable&lt;T&gt;</para>
        ///     <para>PropertyChanged raise on selected scheduler</para>
        /// </summary>
        public static WeakReactiveProperty<T> ToWeakReactiveProperty<T>(this IObservable<T> source,
            IScheduler raiseEventScheduler,
            T initialValue = default(T),
            ReactivePropertyMode mode =
                ReactivePropertyMode.DistinctUntilChanged | ReactivePropertyMode.RaiseLatestValueOnSubscribe) =>
            new WeakReactiveProperty<T>(source, raiseEventScheduler, initialValue, mode);
    }
}