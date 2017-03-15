using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.WeakExtentions;
using Reactive.Bindings.Extensions;

namespace ConsoleApp3
{
    public class Program
    {
        private static StrongSubscribeViewModel vm1;
        private static WeakSubscribeViewModel vm2;
        private static StrongReactivePropertyViewModel vm3;
        private static ReakReactivePropertyViewModel vm4;
        private static NotifyStrongReactivePropertyViewModel vm5;
        private static NotifyWeakReactivePropertyViewModel vm6;

        public static void Main(string[] args)
        {
            IService service = new Service();
            NotifyModel model = new NotifyModel();


            vm1 = new StrongSubscribeViewModel(service);
            vm2 = new WeakSubscribeViewModel(service);
            vm3 = new StrongReactivePropertyViewModel(service);
            vm4 = new ReakReactivePropertyViewModel(service);
            vm5 = new NotifyStrongReactivePropertyViewModel(model);
            vm6 = new NotifyWeakReactivePropertyViewModel(model);



            service.SetTime();
            model.SetTime(0);

            Console.WriteLine();
            GC.Collect();
            Console.WriteLine("GC.Collect");
            Console.WriteLine();

            service.SetTime();
            model.SetTime(1);

            vm1 = null;
            vm2 = null;
            vm3 = null;
            vm4 = null;
            vm5 = null;
            vm6 = null;

            Console.WriteLine();
            GC.Collect();
            Console.WriteLine("GC.Collect");
            Console.WriteLine();

            service.SetTime();
            model.SetTime(2);

            Console.ReadKey();
        }
    }

    public class StrongSubscribeViewModel
    {
        public StrongSubscribeViewModel(IService service)
        {
            Time = service.Time.Select(x => x.ToString() + "(StrongSubscribe)").ToReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged);
            Time.Subscribe(Console.WriteLine).AddTo(_disposables);
            Time.AddTo(_disposables);
        }
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        public IReactiveProperty<string> Time { get; }
    }
    public class WeakSubscribeViewModel
    {
        public WeakSubscribeViewModel(IService service)
        {
            Time = service.Time.Select(x => x.ToString() + "(WeakSubscribe)").ToWeakReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged);
            Time.WeakSubscribe(new AnonymousObserver<string>(Console.WriteLine)).AddTo(_disposables);
            Time.AddTo(_disposables);
        }
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        public IReactiveProperty<string> Time { get; }
    }
    public class StrongReactivePropertyViewModel
    {
        public StrongReactivePropertyViewModel(IService service)
        {
            Time = service.Time.Select((x => x.ToString() + "(StrongReactiveProperty)")).ToReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged);
            Time.Subscribe(Console.WriteLine);
        }
        public IReactiveProperty<string> Time { get; }
    }
    public class ReakReactivePropertyViewModel
    {
        public ReakReactivePropertyViewModel(IService service)
        {
            Time = service.Time.Select((x => x.ToString() + "(WeakReactiveProperty)")).ToWeakReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged);
            Time.Subscribe(Console.WriteLine);
        }
        public IReactiveProperty<string> Time { get; }
    }

    public interface IService
    {
        IObservable<int> Time { get; }
        void SetTime();
    }
    public class Service : IService
    {
        public Service()
        {
            _timesubject = new Subject<int>();
        }
        private int _time = 0;
        private Subject<int> _timesubject;
        public IObservable<int> Time => _timesubject;

        public void SetTime()
        {
            _timesubject.OnNext(_time ++);
        }
    }

    public class WeakService : IService
    {
        public WeakService()
        {
            _time = new WeakReactiveProperty<int>();
        }
        private WeakReactiveProperty<int> _time;
        public IObservable<int> Time => _time;
        public void SetTime()
        {
            _time.Value = ++_time.Value;
        }
    }

    public class NotifyModel : BindableBase
    {
        private int _time;

        public int Time
        {
            get { return _time; }
            private set { SetProperty(ref _time, value); }
        }

        public void SetTime(int time)
        {
            Time = time;
        }
    }

    public class NotifyStrongReactivePropertyViewModel
    {
        public NotifyStrongReactivePropertyViewModel(NotifyModel model)
        {
            Time =
                model.ObserveProperty(x => x.Time)
                    .Select(x => $"{x} - NotifyStrongReactivePropertyViewModel")
                    .ToReactiveProperty();

            Time.Subscribe(Console.WriteLine);
        }

        public IReactiveProperty<string> Time { get; }
    }
    public class NotifyWeakReactivePropertyViewModel
    {
        public NotifyWeakReactivePropertyViewModel(NotifyModel model)
        {
            Time =
                model.ObserveProperty(x => x.Time)
                    .Select(x => $"{x} - NotifyWeakReactivePropertyViewModel")
                    .ToWeakReactiveProperty();

            Time.Subscribe(Console.WriteLine);
        }

        public IReactiveProperty<string> Time { get; }
    }

    public abstract class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals((object)storage, (object)value))
                return false;

            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
