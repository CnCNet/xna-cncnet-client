using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    /// <summary>
    /// This gui creator helps in the registration of XNAControl based controls that can be used via dependency injection
    /// or through the INI system.
    /// </summary>
    public static class ClientGUICreator
    {
        private static List<Type> controlTypes = new();

        private static IServiceProvider serviceProvider;

        /// <summary>
        /// Adds a control type as a singleton to our list of known control types.
        ///
        /// When a control is added as singleton, the same instance will be returned every time one is requested by the control's name.
        /// </summary>
        /// <param name="serviceCollection">Service collection for our dependency injection.</param>
        /// <param name="controlType">The control type to add.</param>
        /// <returns>IServiceCollection.</returns>
        public static IServiceCollection AddSingletonXnaControl<T>(this IServiceCollection serviceCollection)
        {
            Type controlType = typeof(T);
            AddXnaControl(controlType);
            return serviceCollection.AddSingleton(controlType, provider => GetXnaControl(provider, controlType.Name));
        }

        /// <summary>
        /// Adds a control type as a transient to our list of known control types.
        ///
        /// When a control is added as transient, a new instance will be instantiated every time one is requested by the control's name.
        /// </summary>
        /// <param name="serviceCollection">Service collection for our dependency injection.</param>
        /// <param name="controlType">The control type to add.</param>
        /// <returns>IServiceCollection.</returns>
        public static IServiceCollection AddTransientXnaControl<T>(this IServiceCollection serviceCollection)
        {
            Type controlType = typeof(T);
            AddXnaControl(controlType);
            return serviceCollection.AddTransient(controlType, provider => GetXnaControl(provider, controlType.Name));
        }

        /// <summary>
        /// This is typically called during control initialization via the INI UI system.
        /// </summary>
        /// <param name="controlTypeName">The name of the control to instantiate.</param>
        /// <returns>XNAControl instance.</returns>
        public static XNAControl GetXnaControl(string controlTypeName) => GetXnaControl(serviceProvider, controlTypeName);

        /// <summary>
        /// Adds the control type to our list of known controls for instantiation.
        /// </summary>
        /// <param name="controlType">The control type to add.</param>
        /// <exception cref="Exception">
        /// If this control is not a sub-class of XNAControl or is not an XNAControl itself.
        /// OR, this component type is added more than once.
        /// </exception>
        private static void AddXnaControl(Type controlType)
        {
            if (!controlType.IsSubclassOf(typeof(XNAControl)) && controlType != typeof(XNAControl))
                throw new Exception($"{controlType.Name} is not a sub class of {nameof(XNAControl)}");

            ValidateNonDuplicateControlType(controlType);

            controlTypes.Add(controlType);
        }

        /// <summary>
        /// Because the INI system retrieves controls by its <see cref="Type.Name"/>, we need to make sure that
        /// duplicates are not being registered with the same base name as another control.
        /// </summary>
        /// <param name="controlType">The Type to validate.</param>
        /// <exception cref="Exception">If another control was registered with the same name.</exception>
        private static void ValidateNonDuplicateControlType(Type controlType)
        {
            if (controlTypes.Any(c => c.Name == controlType.Name))
                throw new Exception($"A control type with name {controlType.Name} has already been registered.");
        }

        /// <summary>
        /// This is the "factory" that is used to instantiate a control.
        ///
        /// If this function is called for a singleton, it will only be called ONCE for a given <see cref="controlTypeName"/>
        /// </summary>
        /// <param name="provider">Our dependency injection service provider.</param>
        /// <param name="controlTypeName">The name of the control type to instantiate.</param>
        /// <returns>XNAControl instance.</returns>
        /// <exception cref="Exception">If the control type was not registered with our service provider.</exception>
        private static XNAControl GetXnaControl(IServiceProvider provider, string controlTypeName)
        {
            serviceProvider ??= provider;
            Type controlType = controlTypes.SingleOrDefault(control => control.Name == controlTypeName);
            if (controlType == null)
                throw new Exception($"Control type {controlTypeName} was not registered with ServiceCollection in GameClass");

            ConstructorInfo constructor = controlType.GetConstructors().First();
            IEnumerable<object> parameterInstances = constructor.GetParameters().Select(param => GetTypeInstance(param.ParameterType));

            return (XNAControl)constructor.Invoke(parameterInstances.ToArray());
        }

        /// <summary>
        /// Attempts to get an instance of a specific type from our serviced provider.
        /// </summary>
        /// <param name="type">The type to instantiate.</param>
        /// <returns>An instance of the type specified.</returns>
        /// <exception cref="Exception">If the type was not registered with our service provider.</exception>
        private static object GetTypeInstance(Type type)
            => serviceProvider.GetService(type) ?? throw new Exception($"Control type {type.Name} was not registered with ServiceCollection in GameClass");
    }
}