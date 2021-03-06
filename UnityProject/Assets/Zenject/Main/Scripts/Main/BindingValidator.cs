using System;
using System.Collections.Generic;
using System.Linq;
using ModestTree;

namespace Zenject
{
    internal static class BindingValidator
    {
        public static IEnumerable<ZenjectResolveException> ValidateContract(DiContainer container, InjectContext context)
        {
            var matches = container.GetProviderMatches(context);

            if (matches.Count == 1)
            {
                foreach (var error in matches.Single().ValidateBinding(context))
                {
                    yield return error;
                }
            }
            else
            {
                if (ReflectionUtil.IsGenericList(context.MemberType))
                {
                    var subContext = context.ChangeMemberType(context.MemberType.GetGenericArguments().Single());

                    matches = container.GetProviderMatches(subContext);

                    if (matches.IsEmpty())
                    {
                        if (!context.Optional)
                        {
                            if (container.FallbackProvider != null)
                            {
                                foreach (var error in container.FallbackProvider.ValidateBinding(context))
                                {
                                    yield return error;
                                }
                            }
                            else
                            {
                                yield return new ZenjectResolveException(
                                    "Could not find dependency with type 'List<{0}>'{1}.  If the empty list is also valid, you can allow this by using the [InjectOptional] attribute.' \nObject graph:\n{2}"
                                    .Fmt(
                                        subContext.MemberType.Name(),
                                        (context.ParentType == null ? "" : " when injecting into '{0}'".Fmt(context.ParentType.Name())),
                                        DiContainer.GetCurrentObjectGraph()));
                            }
                        }
                    }
                    else
                    {
                        foreach (var match in matches)
                        {
                            foreach (var error in match.ValidateBinding(context))
                            {
                                yield return error;
                            }
                        }
                    }
                }
                else
                {
                    if (!context.Optional)
                    {
                        if (matches.IsEmpty())
                        {
                            if (container.FallbackProvider != null)
                            {
                                foreach (var error in container.FallbackProvider.ValidateBinding(context))
                                {
                                    yield return error;
                                }
                            }
                            else
                            {
                                yield return new ZenjectResolveException(
                                    "Could not find required dependency with type '{0}'{1} \nObject graph:\n{2}"
                                    .Fmt(
                                        context.MemberType.Name(),
                                        (context.ParentType == null ? "" : " when injecting into '{0}'".Fmt(context.ParentType.Name())),
                                        DiContainer.GetCurrentObjectGraph()));
                            }
                        }
                        else
                        {
                            yield return new ZenjectResolveException(
                                "Found multiple matches when only one was expected for dependency with type '{0}'{1} \nObject graph:\n{2}"
                                .Fmt(
                                    context.MemberType.Name(),
                                    (context.ParentType == null ? "" : " when injecting into '{0}'".Fmt(context.ParentType.Name())),
                                    DiContainer.GetCurrentObjectGraph()));
                        }
                    }
                }
            }
        }

        public static IEnumerable<ZenjectResolveException> ValidateObjectGraph(
            DiContainer container, Type concreteType, params Type[] extras)
        {
            using (container.PushLookup(concreteType))
            {
                var typeInfo = TypeAnalyzer.GetInfo(concreteType);
                var extrasList = extras.ToList();

                foreach (var dependInfo in typeInfo.AllInjectables)
                {
                    Assert.IsEqual(dependInfo.ParentType, concreteType);

                    if (TryTakingFromExtras(dependInfo.MemberType, extrasList))
                    {
                        continue;
                    }

                    var context = dependInfo.CreateInjectContext(container, null);

                    foreach (var error in ValidateContract(container, context))
                    {
                        yield return error;
                    }
                }

                if (!extrasList.IsEmpty())
                {
                    yield return new ZenjectResolveException(
                        "Found unnecessary extra parameters passed when injecting into '{0}' with types '{1}'.  \nObject graph:\n{2}"
                        .Fmt(concreteType.Name(), String.Join(",", extrasList.Select(x => x.Name()).ToArray()), DiContainer.GetCurrentObjectGraph()));
                }
            }
        }

        static bool TryTakingFromExtras(Type contractType, List<Type> extrasList)
        {
            foreach (var extraType in extrasList)
            {
                if (extraType.DerivesFromOrEqual(contractType))
                {
                    var removed = extrasList.Remove(extraType);
                    Assert.That(removed);
                    return true;
                }
            }

            return false;
        }
    }
}
