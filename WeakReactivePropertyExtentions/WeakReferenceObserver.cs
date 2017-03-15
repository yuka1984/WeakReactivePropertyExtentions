using System;
using System.Reactive;
using System.Reactive.PlatformServices;

namespace Reactive.Bindings.WeakExtentions
{
    internal static class Stubs<T>
    {
        public static readonly Action<T> Ignore = _ => { };
        public static readonly Func<T, T> I = _ => _;
    }
    internal static class Stubs
    {
        public static readonly Action Nop = () => { };
        public static readonly Action<Exception> Throw = ex => { ex.Throw(); };
    }
    internal static class ExceptionHelpers
    {
        private static Lazy<IExceptionServices> s_services = new Lazy<IExceptionServices>(Initialize);

        public static void Throw(this Exception exception)
        {
            s_services.Value.Rethrow(exception);
        }

        public static void ThrowIfNotNull(this Exception exception)
        {
            if (exception != null)
                s_services.Value.Rethrow(exception);
        }

        private static IExceptionServices Initialize()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return PlatformEnlightenmentProvider.Current.GetService<IExceptionServices>() ?? new DefaultExceptionServices();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
    //
    // WARNING: This code is kept *identically* in two places. One copy is kept in System.Reactive.Core for non-PLIB platforms.
    //          Another copy is kept in System.Reactive.PlatformServices to enlighten the default lowest common denominator
    //          behavior of Rx for PLIB when used on a more capable platform.
    //
    internal class DefaultExceptionServices/*Impl*/ : IExceptionServices
    {
        public void Rethrow(Exception exception)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    public class WeakReferenceObserver<T> : ObserverBase<T>
    {
        public readonly WeakReference<Action<T>> _onNext;
        public readonly WeakReference<Action<Exception>> _onError;
        public readonly WeakReference<Action> _onCompleted;

        /// <summary>
        /// Creates an observer from the specified OnNext, OnError, and OnCompleted actions.
        /// </summary>
        /// <param name="onNext">Observer's OnNext action implementation.</param>
        /// <param name="onError">Observer's OnError action implementation.</param>
        /// <param name="onCompleted">Observer's OnCompleted action implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="onNext"/> or <paramref name="onError"/> or <paramref name="onCompleted"/> is null.</exception>
        public WeakReferenceObserver(Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (onNext == null)
                throw new ArgumentNullException(nameof(onNext));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));

            _onNext = new WeakReference<Action<T>>(onNext);
            _onError = new WeakReference<Action<Exception>>(onError);
            _onCompleted = new WeakReference<Action>(onCompleted);
            
        }

        /// <summary>
        /// Creates an observer from the specified OnNext action.
        /// </summary>
        /// <param name="onNext">Observer's OnNext action implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="onNext"/> is null.</exception>
        public WeakReferenceObserver(Action<T> onNext)
            : this(onNext, Stubs.Throw, Stubs.Nop)
        {
        }

        /// <summary>
        /// Creates an observer from the specified OnNext and OnError actions.
        /// </summary>
        /// <param name="onNext">Observer's OnNext action implementation.</param>
        /// <param name="onError">Observer's OnError action implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="onNext"/> or <paramref name="onError"/> is null.</exception>
        public WeakReferenceObserver(Action<T> onNext, Action<Exception> onError)
            : this(onNext, onError, Stubs.Nop)
        {
        }

        /// <summary>
        /// Creates an observer from the specified OnNext and OnCompleted actions.
        /// </summary>
        /// <param name="onNext">Observer's OnNext action implementation.</param>
        /// <param name="onCompleted">Observer's OnCompleted action implementation.</param>
        /// <exception cref="ArgumentNullException"><paramref name="onNext"/> or <paramref name="onCompleted"/> is null.</exception>
        public WeakReferenceObserver(Action<T> onNext, Action onCompleted)
            : this(onNext, Stubs.Throw, onCompleted)
        {
        }

        protected override void OnNextCore(T value)
        {
            Action<T> onnext = null;
            if (_onNext?.TryGetTarget(out onnext) == true)
            {
                onnext(value);
            }
        }

        protected override void OnErrorCore(Exception error)
        {
            Action<Exception> onerror = null;
            if (_onError?.TryGetTarget(out onerror) == true)
            {
                onerror(error);
            }
        }

        protected override void OnCompletedCore()
        {
            Action oncomplete = null;
            if (_onCompleted?.TryGetTarget(out oncomplete) == true)
            {
                oncomplete();
            }
        }
    }

}