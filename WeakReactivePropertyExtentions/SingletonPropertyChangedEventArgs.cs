using System.ComponentModel;

namespace Reactive.Bindings.WeakExtentions
{
    internal static class SingletonPropertyChangedEventArgs
    {
        public static readonly PropertyChangedEventArgs Value =
            new PropertyChangedEventArgs(nameof(ReactiveProperty<object>.Value));
    }
}