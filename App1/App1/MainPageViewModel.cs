using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace App1
{
    public class MainPageViewModel
    {

        IService _service;
        public MainPageViewModel(IService service, INavigation navigation)
        {
            _service = service;
            Time = service.Time.Do(x => { Debug.WriteLine($"{x} Debug"); }).Select(x => $"{x} 回目").ToReadOnlyReactiveProperty();

            CountUpCommand = new AsyncReactiveCommand();
            CountUpCommand.Subscribe(async _ => {
                GC.Collect();
                await Task.Delay(500);
                _service.SetTime();
            });

            GoNextPageCommand = new AsyncReactiveCommand();
            GoNextPageCommand.Subscribe(async _ => {
                await navigation.PushAsync(new MainPage { BindingContext = new MainPageViewModel(_service, navigation) });

            });
        }

        public ReadOnlyReactiveProperty<string> Time { get; }

        public AsyncReactiveCommand CountUpCommand { get; }

        public AsyncReactiveCommand GoNextPageCommand { get; }

        
    }
}
