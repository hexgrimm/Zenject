using System;
using UnityEngine;

namespace Zenject
{
    public class ValueBinder<TContract> : BinderGeneric<TContract> where TContract : struct
    {
        public ValueBinder(DiContainer container, string identifier)
            : base(container, identifier)
        {
        }

        public BindingConditionSetter To(TContract value)
        {
            return ToProvider(new InstanceProvider(typeof(TContract), value));
        }

        public override BindingConditionSetter ToProvider(ProviderBase provider)
        {
            var conditionSetter = base.ToProvider(provider);

            // Also bind to nullable primitives
            // this is useful so that we can have optional primitive dependencies
            _container.RegisterProvider(provider, new BindingId(typeof(Nullable<TContract>), _bindIdentifier));

            return conditionSetter;
        }
    }
}
