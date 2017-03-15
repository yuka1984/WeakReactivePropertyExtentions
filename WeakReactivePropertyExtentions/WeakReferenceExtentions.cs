using System;

namespace Reactive.Bindings.WeakExtentions
{
    public static class WeakReferenceExtentions
    {
        public static IDisposable WeakSubscribe<T>(this IObservable<T> observable, IObserver<T> observer)
        {
            return new WeakSubscription<T>(observable, observer);
        }

        public class WeakSubscription<T> : IDisposable, IObserver<T>
        {
            private readonly WeakReference reference;
            private readonly IDisposable subscription;
            private bool disposed;

            public WeakSubscription(IObservable<T> observable, IObserver<T> observer)
            {
                reference = new WeakReference(observer, true);
                subscription = observable.Subscribe(this);
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    subscription.Dispose();
                }
            }

            void IObserver<T>.OnCompleted()
            {
                IObserver<T> observer = (IObserver<T>) reference.Target;
                if (observer != null) observer.OnCompleted();
                else Dispose();
            }

            void IObserver<T>.OnError(Exception error)
            {
                IObserver<T> observer = (IObserver<T>) reference.Target;
                if (observer != null) observer.OnError(error);
                else Dispose();
            }

            void IObserver<T>.OnNext(T value)
            {
                IObserver<T> observer = (IObserver<T>) reference.Target;
                if (observer != null) observer.OnNext(value);
                else Dispose();
            }
        }
    }
}