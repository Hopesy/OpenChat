using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenChat.Entitys
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
