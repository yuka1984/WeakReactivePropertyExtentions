using System.ComponentModel;

namespace Reactive.Bindings.WeakExtentions
{
    internal static class SingletonDataErrorsChangedEventArgs
    {
        public static readonly DataErrorsChangedEventArgs Value =
            new DataErrorsChangedEventArgs(nameof(WeakReactiveProperty<object>.Value));
    }
}