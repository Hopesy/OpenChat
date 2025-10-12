using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenChat.Models
{
    public partial class ValueWrapper<T> : ObservableObject
    {
        public ValueWrapper(T value)
        {
            _value = value;
        }

        [ObservableProperty]
        private T _value;
    }
}
