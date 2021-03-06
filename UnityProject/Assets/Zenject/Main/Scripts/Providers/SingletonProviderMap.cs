using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModestTree;

namespace Zenject
{
    public class SingletonProviderMap
    {
        Dictionary<SingletonId, SingletonLazyCreator> _creators = new Dictionary<SingletonId, SingletonLazyCreator>();
        DiContainer _container;

        public SingletonProviderMap(DiContainer container)
        {
            _container = container;
        }

        internal IEnumerable<SingletonLazyCreator> Creators
        {
            get
            {
                return _creators.Values;
            }
        }

        internal void RemoveCreator(SingletonId id)
        {
            bool success = _creators.Remove(id);
            Assert.That(success);
        }

        SingletonLazyCreator AddCreatorFromMethod<TConcrete>(
            string identifier, Func<DiContainer, TConcrete> method)
        {
            SingletonLazyCreator creator;

            var id = new SingletonId(identifier, typeof(TConcrete), null);

            if (_creators.ContainsKey(id))
            {
                throw new ZenjectBindException(
                    "Found multiple singleton instances bound to type '{0}'".Fmt(typeof(TConcrete)));
            }

            creator = new SingletonLazyCreator(
                _container, this, id, (container) => method(container));

            _creators.Add(id, creator);

            creator.IncRefCount();
            return creator;
        }

        SingletonLazyCreator AddCreator(SingletonId id)
        {
            SingletonLazyCreator creator;

            if (!_creators.TryGetValue(id, out creator))
            {
                creator = new SingletonLazyCreator(_container, this, id);
                _creators.Add(id, creator);
            }

            creator.IncRefCount();
            return creator;
        }

        public ProviderBase CreateProviderFromPrefab(string identifier, Type concreteType, GameObject prefab)
        {
            return new SingletonProvider(
                _container, AddCreator(new SingletonId(identifier, concreteType, prefab)));
        }

        public ProviderBase CreateProviderFromMethod<TConcrete>(
            string identifier, Func<DiContainer, TConcrete> method)
        {
            return new SingletonProvider(_container, AddCreatorFromMethod(identifier, method));
        }

        public ProviderBase CreateProviderFromType(string identifier, Type concreteType)
        {
            return new SingletonProvider(
                _container, AddCreator(new SingletonId(identifier, concreteType, null)));
        }

        public ProviderBase CreateProviderFromInstance<TConcrete>(string identifier, TConcrete instance)
        {
            return CreateProviderFromInstance(identifier, typeof(TConcrete), instance);
        }

        public ProviderBase CreateProviderFromInstance(string identifier, Type concreteType, object instance)
        {
            Assert.That(instance != null || _container.AllowNullBindings);

            if (instance != null)
            {
                Assert.That(instance.GetType() == concreteType);
            }

            var creator = AddCreator(new SingletonId(identifier, concreteType, null));

            if (creator.HasInstance())
            {
                throw new ZenjectBindException("Found multiple singleton instances bound to the type '{0}'".Fmt(concreteType.Name()));
            }

            creator.SetInstance(instance);

            return new SingletonProvider(_container, creator);
        }
    }
}
