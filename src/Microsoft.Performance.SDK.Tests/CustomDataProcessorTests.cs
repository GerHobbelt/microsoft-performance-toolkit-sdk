// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Performance.SDK.Extensibility;
using Microsoft.Performance.SDK.Processing;
using Microsoft.Performance.Testing;
using Microsoft.Performance.Testing.SDK;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Performance.SDK.Tests
{
    [TestClass]
    public class CustomDataProcessorTests
    {
        private ProcessorOptions Options { get; set; }
        private IApplicationEnvironment ApplicationEnvironment { get; set; }
        private Dictionary<TableDescriptor, Action<ITableBuilder>> TableDescriptorToBuildAction { get; set; }
        private IProcessorEnvironment ProcessorEnvironment { get; set; }
        private List<TableDescriptor> MetadataTables { get; set; }

        private MockProcessor Sut { get; set; }

        [TestInitialize]
        public void Setup()
        {
            this.Options = new ProcessorOptions(
                new[]
                {
                    new OptionInstance(
                        new Option('r', "test"),
                        "face"),
                });
            this.ApplicationEnvironment = new StubApplicationEnvironment();
            this.ProcessorEnvironment = Any.ProcessorEnvironment();
            this.TableDescriptorToBuildAction = new Dictionary<TableDescriptor, Action<ITableBuilder>>
            {
                [Any.TableDescriptor()] = (tableBuilder) => { },
                [Any.TableDescriptor()] = (tableBuilder) => { },
                [Any.TableDescriptor()] = (tableBuilder) => { },
                [Any.MetadataTableDescriptor()] = (tableBuilder) => { },
                [Any.MetadataTableDescriptor()] = (tableBuilder) => { },
            };

            this.MetadataTables = this.TableDescriptorToBuildAction.Keys.Where(x => x.IsMetadataTable).ToList();

            this.Sut = new MockProcessor(
                this.Options,
                this.ApplicationEnvironment,
                this.ProcessorEnvironment,
                this.TableDescriptorToBuildAction,
                this.MetadataTables);
        }

        [TestMethod]
        [UnitTest]
        public void OptionsHydratedCorrectly()
        {
            Assert.AreEqual(this.Options, this.Sut.ExposedOptions);
        }

        [TestMethod]
        [UnitTest]
        public void ApplicationEnvironmentHydratedCorrectly()
        {
            Assert.AreEqual(this.ApplicationEnvironment, this.Sut.ExposedApplicationEnvironment);
        }

        [TestMethod]
        [UnitTest]
        public void TableDescriptorToTypeHydratedCorrectly()
        {
            Assert.AreEqual(this.TableDescriptorToBuildAction, this.Sut.ExposedTableDescriptorToBuildAction);
        }

        [TestMethod]
        [UnitTest]
        public void MetadataTablesNotEnabledByDefault()
        {
            Assert.IsTrue(this.MetadataTables.Count != 0);
            Assert.AreEqual(0, this.Sut.ExposedEnabledTables.Count);
            foreach (var table in this.MetadataTables)
            {
                Assert.IsFalse(this.Sut.ExposedEnabledTables.Contains(table));
            }
        }

        [TestMethod]
        [UnitTest]
        public void EnableTableAddsToCorrectCollection()
        {
            var tableToEnable = Any.TableDescriptor();

            this.Sut.EnableTable(tableToEnable);

            Assert.AreEqual(1, this.Sut.ExposedEnabledTables.Count);
            Assert.IsTrue(this.Sut.ExposedEnabledTables.Contains(tableToEnable));
        }

        [TestMethod]
        [UnitTest]
        public void BuildTableDelegatesCorrectly()
        {
            var tableToBuild = this.TableDescriptorToBuildAction.First();
            var tableBuilder = new FakeTableBuilder();

            this.Sut.BuildTable(tableToBuild.Key, tableBuilder);

            Assert.AreEqual(1, this.Sut.BuildTableCoreCalls.Count);
            Assert.AreEqual(tableToBuild.Key, this.Sut.BuildTableCoreCalls[0].Item1);
            Assert.AreEqual(tableToBuild.Value, this.Sut.BuildTableCoreCalls[0].Item2);
            Assert.AreEqual(tableBuilder, this.Sut.BuildTableCoreCalls[0].Item3);
        }

        [TestMethod]
        [UnitTest]
        public void BuildMetadataTablesDelegatesCorrectly()
        {
            var factory = new FakeMetadataTableBuilderFactory();

            this.Sut.BuildMetadataTables(factory);

            Assert.AreEqual(this.MetadataTables.Count, factory.CreatedBuilders.Count);

            Assert.AreEqual(this.MetadataTables.Count, this.Sut.BuildTableCoreCalls.Count);
            for (var i = 0; i < this.MetadataTables.Count; ++i)
            {
                Assert.AreEqual(this.MetadataTables[i], this.Sut.BuildTableCoreCalls[i].Item1);
                Assert.AreEqual(this.TableDescriptorToBuildAction[this.MetadataTables[i]], this.Sut.BuildTableCoreCalls[i].Item2);
                Assert.AreEqual(factory.CreatedBuilders[i], this.Sut.BuildTableCoreCalls[i].Item3);
            }
        }

        public void EnableMetaTableThrows()
        {
            this.Options = new ProcessorOptions(
                new[]
                {
                    new OptionInstance(
                        new Option('r', "test"),
                        "face"),
                });

            this.ApplicationEnvironment = new StubApplicationEnvironment();
            this.ProcessorEnvironment = Any.ProcessorEnvironment();
            this.TableDescriptorToBuildAction = new Dictionary<TableDescriptor, Action<ITableBuilder>>
            {
                [Any.TableDescriptor()] = (tableBuilder) => { },
                [Any.TableDescriptor()] = (tableBuilder) => { },
                [Any.TableDescriptor()] = (tableBuilder) => { },
                [Any.MetadataTableDescriptor()] = (tableBuilder) => { },
                [Any.MetadataTableDescriptor()] = (tableBuilder) => { },
            };

            this.MetadataTables = this.TableDescriptorToBuildAction.Keys.Where(x => x.IsMetadataTable).ToList();

            this.Sut = new MockProcessor(
                this.Options,
                this.ApplicationEnvironment,
                this.ProcessorEnvironment,
                this.TableDescriptorToBuildAction,
                this.MetadataTables);

        }

        private sealed class MockProcessor
            : CustomDataProcessor
        {
            public MockProcessor(
                ProcessorOptions options,
                IApplicationEnvironment applicationEnvironment,
                IProcessorEnvironment processorEnvironment,
                IReadOnlyDictionary<TableDescriptor, Action<ITableBuilder>> allTablesMapping,
                IEnumerable<TableDescriptor> metadataTables)
                : base(options, applicationEnvironment, processorEnvironment, allTablesMapping)
            {
                this.BuildTableCoreCalls = new List<Tuple<TableDescriptor, Action<ITableBuilder>, ITableBuilder>>();
            }

            public ProcessorOptions ExposedOptions => this.Options;

            public IApplicationEnvironment ExposedApplicationEnvironment => this.ApplicationEnvironment;

            public IReadOnlyDictionary<TableDescriptor, Action<ITableBuilder>> ExposedTableDescriptorToBuildAction => this.TableDescriptorToBuildAction;

            public ReadOnlyHashSet<TableDescriptor> ExposedEnabledTables => this.EnabledTables;

            public override DataSourceInfo GetDataSourceInfo()
            {
                return new DataSourceInfo(0, 1, DateTime.UnixEpoch);
            }

            protected override Task ProcessAsyncCore(IProgress<int> progress, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public List<Tuple<TableDescriptor, Action<ITableBuilder>, ITableBuilder>> BuildTableCoreCalls { get; }
            protected override void BuildTableCore(
                TableDescriptor tableDescriptor,
                Action<ITableBuilder> createTable,
                ITableBuilder tableBuilder)
            {
                this.BuildTableCoreCalls.Add(Tuple.Create(tableDescriptor, createTable, tableBuilder));
            }
        }

        private sealed class FakeTableBuilder
            : ITableBuilder
        {
            public IEnumerable<TableConfiguration> BuiltInTableConfigurations => throw new NotImplementedException();

            public TableConfiguration DefaultConfiguration => throw new NotImplementedException();

            public ITableService Service => throw new NotImplementedException();

            public ITableBuilder AddTableCommand(string commandName, TableCommandCallback callback)
            {
                throw new NotImplementedException();
            }

            public ITableBuilder AddTableConfiguration(TableConfiguration configuration)
            {
                throw new NotImplementedException();
            }

            public ITableBuilder SetDefaultTableConfiguration(TableConfiguration configuration)
            {
                throw new NotImplementedException();
            }

            public ITableBuilderWithRowCount SetRowCount(int numberOfRows)
            {
                throw new NotImplementedException();
            }

            public ITableBuilder SetService(ITableService service)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class FakeMetadataTableBuilderFactory
            : IMetadataTableBuilderFactory
        {
            public FakeMetadataTableBuilderFactory()
            {
                this.CreatedBuilders = new List<FakeTableBuilder>();
            }

            public List<FakeTableBuilder> CreatedBuilders { get; }

            public ITableBuilder Create(TableDescriptor tableDescriptor)
            {
                var builder = new FakeTableBuilder();
                this.CreatedBuilders.Add(builder);
                return builder;
            }
        }

        [Table]
        public class TestTable1
        {
            public static readonly string SourceParserId = "SourceId1";

            public static readonly DataCookerPath RequiredDataCooker =
                DataCookerPath.ForSource(SourceParserId, "CookerId1");

            public static TableDescriptor TableDescriptor = new TableDescriptor(
                    Guid.Parse("{C2AC873F-B88E-46A1-A08B-0CDF1D0C9F18}"),
                    "Test Table with Requirements",
                    "This table has required data extensions",
                    "Other",
                    isMetadataTable: true,
                    requiredDataCookers: new List<DataCookerPath> { RequiredDataCooker })
            { Type = typeof(TestTable1) };

            public static void BuildTable(ITableBuilder tableBuilder, IDataExtensionRetrieval dataRetrieval)
            {
            }
        }


    }
}
