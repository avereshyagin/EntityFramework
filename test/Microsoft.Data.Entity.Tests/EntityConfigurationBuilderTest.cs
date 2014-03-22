// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Logging;
using Microsoft.Data.Entity.ChangeTracking;
using Microsoft.Data.Entity.Identity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Services;
using Microsoft.Data.Entity.Storage;
using Moq;
using Xunit;

namespace Microsoft.Data.Entity.Tests
{
    public class EntityConfigurationBuilderTest
    {
        [Fact]
        public void Default_services_are_registered_when_parameterless_constructor_used()
        {
            var configuration = new EntityConfigurationBuilder().BuildConfiguration();

            Assert.IsType<DefaultIdentityGeneratorFactory>(configuration.IdentityGeneratorFactory);
            Assert.IsType<NullLoggerFactory>(configuration.LoggerFactory);
            Assert.IsType<ActiveIdentityGenerators>(configuration.ActiveIdentityGenerators);
            Assert.IsType<EntitySetFinder>(configuration.EntitySetFinder);
            Assert.IsType<EntitySetInitializer>(configuration.EntitySetInitializer);
            Assert.IsType<EntityKeyFactorySource>(configuration.EntityKeyFactorySource);
            Assert.IsType<ClrCollectionAccessorSource>(configuration.ClrCollectionAccessorSource);
            Assert.IsType<ClrPropertyGetterSource>(configuration.ClrPropertyGetterSource);
            Assert.IsType<ClrPropertySetterSource>(configuration.ClrPropertySetterSource);
            Assert.IsType<EntitySetSource>(configuration.EntitySetSource);
            Assert.IsType<EntityMaterializerSource>(configuration.EntityMaterializerSource);
        }

        [Fact]
        public void Default_context_scoped_services_are_registered_when_parameterless_constructor_used()
        {
            var configuration = new EntityConfigurationBuilder().BuildConfiguration();

            using (var context = new EntityContext(configuration))
            {
                var contextConfiguration = context.Configuration;

                Assert.IsType<StateEntryFactory>(contextConfiguration.StateEntryFactory);
                Assert.IsType<StateEntryNotifier>(contextConfiguration.StateEntryNotifier);
                Assert.IsType<ContextEntitySets>(contextConfiguration.ContextEntitySets);
                Assert.IsType<StateManager>(contextConfiguration.StateManager);
                Assert.IsType<NavigationFixer>(contextConfiguration.EntityStateListeners.Single());
            }
        }

        [Fact]
        public void Can_get_singleton_service_from_scoped_configuration()
        {
            var configuration = new EntityConfigurationBuilder().BuildConfiguration();

            using (var context = new EntityContext(configuration))
            {
                var contextConfiguration = context.Configuration;

                Assert.IsType<EntityMaterializerSource>(contextConfiguration.EntityMaterializerSource);
            }
        }

        [Fact]
        public void Can_start_with_custom_services_by_passing_in_service_collection()
        {
            var identityGeneratorFactory = Mock.Of<IdentityGeneratorFactory>();

            var serviceCollection = new ServiceCollection()
                .AddInstance<IdentityGeneratorFactory>(identityGeneratorFactory);

            var configuration = new EntityConfigurationBuilder(serviceCollection).BuildConfiguration();

            Assert.Same(identityGeneratorFactory, configuration.IdentityGeneratorFactory);
        }

        [Fact]
        public void Can_replace_already_registered_service_with_new_service()
        {
            var myService = Mock.Of<IModelSource>();

            var configuration = new EntityConfigurationBuilder()
                .UseModelSource(myService)
                .BuildConfiguration();

            Assert.Same(myService, configuration.ModelSource);
        }

        [Fact]
        public void Can_add_to_collection_of_services()
        {
            var myService = Mock.Of<IEntityStateListener>();

            var builder = new EntityConfigurationBuilder();
            builder.ServiceCollection.AddInstance<IEntityStateListener>(myService);
            var configuration = builder.BuildConfiguration();

            using (var context = new EntityContext(configuration))
            {
                Assert.Equal(
                    new[] { myService.GetType(), typeof(NavigationFixer) },
                    context.Configuration.EntityStateListeners.Select(l => l.GetType()).OrderBy(t => t.Name).ToArray());
            }
        }

        [Fact]
        public void Can_get_and_use_service_collection_directly()
        {
            var myService = Mock.Of<IModelSource>();

            var builder = new EntityConfigurationBuilder();
            builder.ServiceCollection.AddInstance<IModelSource>(myService);
            var configuration = builder.BuildConfiguration();

            Assert.Same(myService, configuration.ModelSource);
        }

        [Fact]
        public void Can_set_known_singleton_services_using_instance_sugar()
        {
            var identityGenerators = Mock.Of<ActiveIdentityGenerators>();
            var collectionSource = Mock.Of<ClrCollectionAccessorSource>();
            var getterSource = Mock.Of<ClrPropertyGetterSource>();
            var setterSource = Mock.Of<ClrPropertySetterSource>();
            var dataStore = Mock.Of<DataStore>();
            var keyFactorySource = Mock.Of<EntityKeyFactorySource>();
            var materializerSource = Mock.Of<EntityMaterializerSource>();
            var setFinder = Mock.Of<EntitySetFinder>();
            var setInitializer = Mock.Of<EntitySetInitializer>();
            var setSource = Mock.Of<EntitySetSource>();
            var generatorFactory = Mock.Of<IdentityGeneratorFactory>();
            var loggerFactory = Mock.Of<ILoggerFactory>();
            var model = Mock.Of<IModel>();
            var modelSource = Mock.Of<IModelSource>();

            var configuration = new EntityConfigurationBuilder()
                .UseActiveIdentityGenerators(identityGenerators)
                .UseClrCollectionAccessorSource(collectionSource)
                .UseClrPropertyGetterSource(getterSource)
                .UseClrPropertySetterSource(setterSource)
                .UseDataStore(dataStore)
                .UseEntityKeyFactorySource(keyFactorySource)
                .UseEntityMaterializerSource(materializerSource)
                .UseEntitySetFinder(setFinder)
                .UseEntitySetInitializer(setInitializer)
                .UseEntitySetSource(setSource)
                .UseIdentityGeneratorFactory(generatorFactory)
                .UseLoggerFactory(loggerFactory)
                .UseModel(model)
                .UseModelSource(modelSource)
                .BuildConfiguration();

            Assert.Same(identityGenerators, configuration.ActiveIdentityGenerators);
            Assert.Same(collectionSource, configuration.ClrCollectionAccessorSource);
            Assert.Same(getterSource, configuration.ClrPropertyGetterSource);
            Assert.Same(setterSource, configuration.ClrPropertySetterSource);
            Assert.Same(dataStore, configuration.DataStore);
            Assert.Same(keyFactorySource, configuration.EntityKeyFactorySource);
            Assert.Same(materializerSource, configuration.EntityMaterializerSource);
            Assert.Same(setFinder, configuration.EntitySetFinder);
            Assert.Same(setInitializer, configuration.EntitySetInitializer);
            Assert.Same(setSource, configuration.EntitySetSource);
            Assert.Same(generatorFactory, configuration.IdentityGeneratorFactory);
            Assert.Same(loggerFactory, configuration.LoggerFactory);
            Assert.Same(modelSource, configuration.ModelSource);
        }

        [Fact]
        public void Can_set_known_singleton_services_using_type_activation()
        {
            var configuration = new EntityConfigurationBuilder()
                .UseActiveIdentityGenerators<FakeActiveIdentityGenerators>()
                .UseClrCollectionAccessorSource<FakeClrCollectionAccessorSource>()
                .UseClrPropertyGetterSource<FakeClrPropertyGetterSource>()
                .UseClrPropertySetterSource<FakeClrPropertySetterSource>()
                .UseDataStore<FakeDataStore>()
                .UseEntityKeyFactorySource<FakeEntityKeyFactorySource>()
                .UseEntityMaterializerSource<FakeEntityMaterializerSource>()
                .UseEntitySetFinder<FakeEntitySetFinder>()
                .UseEntitySetInitializer<FakeEntitySetInitializer>()
                .UseEntitySetSource<FakeEntitySetSource>()
                .UseEntityStateListener<FakeEntityStateListener>()
                .UseIdentityGeneratorFactory<FakeIdentityGeneratorFactory>()
                .UseLoggerFactory<FakeLoggerFactory>()
                .UseModelSource<FakeModelSource>()
                .BuildConfiguration();

            Assert.IsType<FakeActiveIdentityGenerators>(configuration.ActiveIdentityGenerators);
            Assert.IsType<FakeClrCollectionAccessorSource>(configuration.ClrCollectionAccessorSource);
            Assert.IsType<FakeClrPropertyGetterSource>(configuration.ClrPropertyGetterSource);
            Assert.IsType<FakeClrPropertySetterSource>(configuration.ClrPropertySetterSource);
            Assert.IsType<FakeDataStore>(configuration.DataStore);
            Assert.IsType<FakeEntityKeyFactorySource>(configuration.EntityKeyFactorySource);
            Assert.IsType<FakeEntityMaterializerSource>(configuration.EntityMaterializerSource);
            Assert.IsType<FakeEntitySetFinder>(configuration.EntitySetFinder);
            Assert.IsType<FakeEntitySetInitializer>(configuration.EntitySetInitializer);
            Assert.IsType<FakeEntitySetSource>(configuration.EntitySetSource);
            Assert.IsType<FakeIdentityGeneratorFactory>(configuration.IdentityGeneratorFactory);
            Assert.IsType<FakeLoggerFactory>(configuration.LoggerFactory);
            Assert.IsType<FakeModelSource>(configuration.ModelSource);
        }

        [Fact]
        public void Can_set_known_context_scoped_services_using_type_activation()
        {
            var configuration = new EntityConfigurationBuilder()
                .UseStateEntryFactory<FakeStateEntryFactory>()
                .UseStateEntryNotifier<FakeStateEntryNotifier>()
                .UseContextEntitySets<FakeContextEntitySets>()
                .UseStateManager<FakeStateManager>()
                .UseEntityStateListener<FakeNavigationFixer>()
                .BuildConfiguration();

            using (var context = new EntityContext(configuration))
            {
                var contextConfiguration = context.Configuration;

                Assert.IsType<FakeStateEntryFactory>(contextConfiguration.StateEntryFactory);
                Assert.IsType<FakeStateEntryNotifier>(contextConfiguration.StateEntryNotifier);
                Assert.IsType<FakeContextEntitySets>(contextConfiguration.ContextEntitySets);
                Assert.IsType<FakeStateManager>(contextConfiguration.StateManager);

                Assert.Equal(
                    new[] { typeof(FakeNavigationFixer), typeof(NavigationFixer) },
                    context.Configuration.EntityStateListeners.Select(l => l.GetType()).OrderBy(t => t.Name).ToArray());
            }
        }

        [Fact]
        public void Replaced_services_are_scoped_appropriately()
        {
            var configuration = new EntityConfigurationBuilder()
                .UseActiveIdentityGenerators<FakeActiveIdentityGenerators>()
                .UseClrCollectionAccessorSource<FakeClrCollectionAccessorSource>()
                .UseClrPropertyGetterSource<FakeClrPropertyGetterSource>()
                .UseClrPropertySetterSource<FakeClrPropertySetterSource>()
                .UseDataStore<FakeDataStore>()
                .UseEntityKeyFactorySource<FakeEntityKeyFactorySource>()
                .UseEntityMaterializerSource<FakeEntityMaterializerSource>()
                .UseEntitySetFinder<FakeEntitySetFinder>()
                .UseEntitySetInitializer<FakeEntitySetInitializer>()
                .UseEntitySetSource<FakeEntitySetSource>()
                .UseEntityStateListener<FakeEntityStateListener>()
                .UseIdentityGeneratorFactory<FakeIdentityGeneratorFactory>()
                .UseLoggerFactory<FakeLoggerFactory>()
                .UseModelSource<FakeModelSource>()
                .UseStateEntryFactory<FakeStateEntryFactory>()
                .UseStateEntryNotifier<FakeStateEntryNotifier>()
                .UseContextEntitySets<FakeContextEntitySets>()
                .UseStateManager<FakeStateManager>()
                .UseEntityStateListener<FakeNavigationFixer>()
                .BuildConfiguration();

            StateEntryFactory stateEntryFactory;
            StateEntryNotifier stateEntryNotifier;
            ContextEntitySets contextEntitySets;
            StateManager stateManager;
            IEntityStateListener entityStateListener;

            var activeIdentityGenerators = configuration.ActiveIdentityGenerators;
            var clrCollectionAccessorSource = configuration.ClrCollectionAccessorSource;
            var clrPropertyGetterSource = configuration.ClrPropertyGetterSource;
            var clrPropertySetterSource = configuration.ClrPropertySetterSource;
            var dataStore = configuration.DataStore;
            var entityKeyFactorySource = configuration.EntityKeyFactorySource;
            var entityMaterializerSource = configuration.EntityMaterializerSource;
            var entitySetFinder = configuration.EntitySetFinder;
            var entitySetInitializer = configuration.EntitySetInitializer;
            var entitySetSource = configuration.EntitySetSource;
            var identityGeneratorFactory = configuration.IdentityGeneratorFactory;
            var loggerFactory = configuration.LoggerFactory;
            var modelSource = configuration.ModelSource;

            using (var context = new EntityContext(configuration))
            {
                var contextConfiguration = context.Configuration;

                stateEntryFactory = contextConfiguration.StateEntryFactory;
                stateEntryNotifier = contextConfiguration.StateEntryNotifier;
                contextEntitySets = contextConfiguration.ContextEntitySets;
                stateManager = contextConfiguration.StateManager;
                entityStateListener = contextConfiguration.EntityStateListeners.OfType<FakeNavigationFixer>().Single();

                Assert.Same(stateEntryFactory, contextConfiguration.StateEntryFactory);
                Assert.Same(stateEntryNotifier, contextConfiguration.StateEntryNotifier);
                Assert.Same(contextEntitySets, contextConfiguration.ContextEntitySets);
                Assert.Same(stateManager, contextConfiguration.StateManager);
                Assert.Same(entityStateListener, contextConfiguration.EntityStateListeners.OfType<FakeNavigationFixer>().Single());

                Assert.Same(activeIdentityGenerators, contextConfiguration.ActiveIdentityGenerators);
                Assert.Same(clrCollectionAccessorSource, contextConfiguration.ClrCollectionAccessorSource);
                Assert.Same(clrPropertyGetterSource, contextConfiguration.ClrPropertyGetterSource);
                Assert.Same(clrPropertySetterSource, contextConfiguration.ClrPropertySetterSource);
                Assert.Same(dataStore, contextConfiguration.DataStore);
                Assert.Same(entityKeyFactorySource, contextConfiguration.EntityKeyFactorySource);
                Assert.Same(entityMaterializerSource, contextConfiguration.EntityMaterializerSource);
                Assert.Same(entitySetFinder, contextConfiguration.EntitySetFinder);
                Assert.Same(entitySetInitializer, contextConfiguration.EntitySetInitializer);
                Assert.Same(entitySetSource, contextConfiguration.EntitySetSource);
                Assert.Same(identityGeneratorFactory, contextConfiguration.IdentityGeneratorFactory);
                Assert.Same(loggerFactory, contextConfiguration.LoggerFactory);
                Assert.Same(modelSource, contextConfiguration.ModelSource);
            }

            using (var context = new EntityContext(configuration))
            {
                var contextConfiguration = context.Configuration;

                Assert.NotSame(stateEntryFactory, contextConfiguration.StateEntryFactory);
                Assert.NotSame(stateEntryNotifier, contextConfiguration.StateEntryNotifier);
                Assert.NotSame(contextEntitySets, contextConfiguration.ContextEntitySets);
                Assert.NotSame(stateManager, contextConfiguration.StateManager);
                Assert.NotSame(entityStateListener, contextConfiguration.EntityStateListeners.OfType<FakeNavigationFixer>().Single());

                Assert.Same(activeIdentityGenerators, contextConfiguration.ActiveIdentityGenerators);
                Assert.Same(clrCollectionAccessorSource, contextConfiguration.ClrCollectionAccessorSource);
                Assert.Same(clrPropertyGetterSource, contextConfiguration.ClrPropertyGetterSource);
                Assert.Same(clrPropertySetterSource, contextConfiguration.ClrPropertySetterSource);
                Assert.Same(dataStore, contextConfiguration.DataStore);
                Assert.Same(entityKeyFactorySource, contextConfiguration.EntityKeyFactorySource);
                Assert.Same(entityMaterializerSource, contextConfiguration.EntityMaterializerSource);
                Assert.Same(entitySetFinder, contextConfiguration.EntitySetFinder);
                Assert.Same(entitySetInitializer, contextConfiguration.EntitySetInitializer);
                Assert.Same(entitySetSource, contextConfiguration.EntitySetSource);
                Assert.Same(identityGeneratorFactory, contextConfiguration.IdentityGeneratorFactory);
                Assert.Same(loggerFactory, contextConfiguration.LoggerFactory);
                Assert.Same(modelSource, contextConfiguration.ModelSource);
            }
        }

        [Fact]
        public void Can_get_replaced_singleton_service_from_scoped_configuration()
        {
            var configuration = new EntityConfigurationBuilder()
                .UseEntityMaterializerSource<FakeEntityMaterializerSource>()
                .BuildConfiguration();

            using (var context = new EntityContext(configuration))
            {
                var contextConfiguration = context.Configuration;

                Assert.IsType<FakeEntityMaterializerSource>(contextConfiguration.EntityMaterializerSource);
            }
        }

        [Fact]
        public void Can_set_IdentityGeneratorFactory_but_fallback_to_service_provider_default()
        {
            var generator1 = new Mock<IIdentityGenerator>().Object;
            var defaultFactoryMock = new Mock<IdentityGeneratorFactory>();
            defaultFactoryMock.Setup(m => m.Create(It.Is<IProperty>(p => p.Name == "Foo"))).Returns(generator1);

            var generator2 = new Mock<IIdentityGenerator>().Object;
            var customFactoryMock = new Mock<IdentityGeneratorFactory>();
            customFactoryMock.Setup(m => m.Create(It.Is<IProperty>(p => p.Name == "Goo"))).Returns(generator2);

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddInstance<IdentityGeneratorFactory>(defaultFactoryMock.Object);

            // This looks silly, but the idea is that I'm getting the default that has been configured
            // so I can then override it. In this test I just created the default myself and I'm adding
            // it myself but this would not normally be the case.
            var defaultFactory = new EntityConfigurationBuilder()
                .UseIdentityGeneratorFactory(defaultFactoryMock.Object)
                .BuildConfiguration()
                .IdentityGeneratorFactory;

            var configuration = new EntityConfigurationBuilder()
                .UseIdentityGeneratorFactory(new OverridingIdentityGeneratorFactory(customFactoryMock.Object, defaultFactory))
                .BuildConfiguration();

            // Should get overridden generator
            Assert.Same(
                generator2,
                configuration.IdentityGeneratorFactory.Create(new Property("Goo", typeof(int), shadowProperty: false)));

            customFactoryMock.Verify(m => m.Create(It.IsAny<IProperty>()), Times.Once);
            defaultFactoryMock.Verify(m => m.Create(It.IsAny<IProperty>()), Times.Never);

            // Should fall back to the service provider
            Assert.Same(
                generator1,
                configuration.IdentityGeneratorFactory.Create(new Property("Foo", typeof(int), shadowProperty: false)));

            customFactoryMock.Verify(m => m.Create(It.IsAny<IProperty>()), Times.Exactly(2));
            defaultFactoryMock.Verify(m => m.Create(It.IsAny<IProperty>()), Times.Once);
        }

        private class FakeActiveIdentityGenerators : ActiveIdentityGenerators
        {
        }

        private class FakeClrCollectionAccessorSource : ClrCollectionAccessorSource
        {
        }

        private class FakeClrPropertyGetterSource : ClrPropertyGetterSource
        {
        }

        private class FakeClrPropertySetterSource : ClrPropertySetterSource
        {
        }

        private class FakeDataStore : DataStore
        {
        }

        private class FakeEntityKeyFactorySource : EntityKeyFactorySource
        {
        }

        private class FakeEntityMaterializerSource : EntityMaterializerSource
        {
        }

        private class FakeEntitySetFinder : EntitySetFinder
        {
        }

        private class FakeEntitySetInitializer : EntitySetInitializer
        {
        }

        private class FakeEntitySetSource : EntitySetSource
        {
        }

        private class FakeEntityStateListener : IEntityStateListener
        {
            public void StateChanging(StateEntry entry, EntityState newState)
            {
            }

            public void StateChanged(StateEntry entry, EntityState oldState)
            {
            }
        }

        private class FakeIdentityGeneratorFactory : IdentityGeneratorFactory
        {
            public override IIdentityGenerator Create(IProperty property)
            {
                return null;
            }
        }

        private class FakeLoggerFactory : ILoggerFactory
        {
            public ILogger Create(string name)
            {
                return null;
            }
        }

        private class FakeModelSource : IModelSource
        {
            public IModel GetModel(EntityContext context)
            {
                return null;
            }
        }

        private class FakeStateEntryFactory : StateEntryFactory
        {
        }

        private class FakeStateEntryNotifier : StateEntryNotifier
        {
        }

        private class FakeContextEntitySets : ContextEntitySets
        {
            public override void InitializeSets(EntityContext context)
            {
            }
        }

        private class FakeStateManager : StateManager
        {
        }

        private class FakeNavigationFixer : NavigationFixer
        {
        }
    }
}
