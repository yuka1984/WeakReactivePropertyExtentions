using Reactive.Bindings.WeakExtentions;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using Reactive.Bindings;

namespace App1
{
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
            _timesubject.OnNext(_time++);
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
}
