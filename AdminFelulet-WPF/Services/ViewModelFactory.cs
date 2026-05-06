using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_AdminFelulet.Services
{
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly IServiceProvider _provider;
        public ViewModelFactory(IServiceProvider provider) { _provider = provider; }
        public T Create<T>() where T : class => _provider.GetRequiredService<T>();
    }
}
