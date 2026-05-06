using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_AdminFelulet.Services
{
    public interface IViewModelFactory
    {
        T Create<T>() where T : class;
    }
}
