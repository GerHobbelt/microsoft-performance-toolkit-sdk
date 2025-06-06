// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Performance.SDK.Processing
{
    /// <summary>
    ///     Represents the configuration of table. The configuration is how the columns
    ///     should be arranged and other details in regards to the display / presentation
    ///     of the data in the table.
    /// </summary>
    public class TableConfiguration
    {
        /// <summary>
        ///     Columns to the left of this column will be pivoted.
        /// </summary>
        public static readonly ColumnConfiguration PivotColumn = new ColumnConfiguration(
            new ColumnMetadata(Guid.Parse("{319E867F-245A-436E-9735-DB620F844EDC}"), "Pivot Column"));

        /// <summary>
        ///     Columns to the right of this column will be graphed.
        /// </summary>
        public static readonly ColumnConfiguration GraphColumn = new ColumnConfiguration(
            new ColumnMetadata(Guid.Parse("{A5CE8FAF-5668-49FC-82BC-D2C482086C06}"), "Graph Column"));

        /// <summary>
        ///     Columns to the left of this column will be frozen in place during horizontal scrolling.
        /// </summary>
        public static readonly ColumnConfiguration LeftFreezeColumn = new ColumnConfiguration(
            new ColumnMetadata(Guid.Parse("{4D35A44E-804B-454E-9375-568543E198C2}"), "Left Freeze Column"));

        /// <summary>
        ///     Columns to the right of this column will be frozen in place during horizontal scrolling.
        /// </summary>
        public static readonly ColumnConfiguration RightFreezeColumn = new ColumnConfiguration(
            new ColumnMetadata(Guid.Parse("{A755CEC5-BBC4-4E9F-BDB3-CB78B56DDCFA}"), "Right Freeze Column"));

        /// <summary>
        ///     Gets all of the available metadata columns. See <see cref="PivotColumn"/>,
        ///     <see cref="GraphColumn"/>, <see cref="LeftFreezeColumn"/>, and <see cref="RightFreezeColumn"/>.
        /// </summary>
        public static readonly IReadOnlyList<ColumnConfiguration> AllMetadataColumns = new[]
            {
                PivotColumn,
                GraphColumn,
                LeftFreezeColumn,
                RightFreezeColumn,
            }.AsReadOnly();

        private readonly IDictionary<string, Guid> columnRoles;

        private ReadOnlyCollection<ColumnConfiguration> columnsRO;

        private ReadOnlyCollection<HighlightEntry> highlightEntriesRO;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TableConfiguration"/>
        ///     class.
        /// </summary>
        public TableConfiguration()
            : this(string.Empty)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TableConfiguration"/>
        ///     class.
        /// <param name="name">Identifies the table configuration.</param>
        /// </summary>
        public TableConfiguration(string name)
        {
            this.Name = name;

            this.columnsRO = new ReadOnlyCollection<ColumnConfiguration>(EmptyArray<ColumnConfiguration>.Instance);
            this.columnRoles = new Dictionary<string, Guid>(StringComparer.Ordinal);
          
            this.ColumnRoles = new ReadOnlyDictionary<string, Guid>(this.columnRoles);

            this.highlightEntriesRO = new ReadOnlyCollection<HighlightEntry>(EmptyArray<HighlightEntry>.Instance);
        }

        /// <summary>
        ///     Gets or sets the name of this configuration.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the type of chart displayed for this table.
        /// </summary>
        public ChartType ChartType { get; set; }

        /// <summary>
        ///     Gets or sets the aggregation over time mode for this table.
        /// </summary>
        public AggregationOverTime AggregationOverTime { get; set; }

        /// <summary>
        ///     Gets or sets the query for initial filtering in this table.
        /// </summary>
        public string InitialFilterQuery { get; set; }

        /// <summary>
        ///     Gets or sets the query for initial expansion in this table.
        /// </summary>
        public string InitialExpansionQuery { get; set; }

        /// <summary>
        ///     Gets or sets the query for initial selection in this table.
        /// </summary>
        public string InitialSelectionQuery { get; set; }

        /// <summary>
        ///     Get or sets whether rows that satisfy <see cref="InitialFilterQuery"/> should be
        ///     kept or removed from the table (i.e. whether <see cref="InitialFilterQuery"/> is a
        ///     "filter-in" or "filter-out" filter). Defaults to <c>true</c>.
        /// </summary>
        public bool InitialFilterShouldKeep { get; set; } = true;

        /// <summary>
        ///     Gets or sets the top value of the graph filter in this value.
        /// </summary>
        public int GraphFilterTopValue { get; set; }

        /// <summary>
        ///     Gets or sets the threshold value of the graph filter in this table.
        /// </summary>
        public double GraphFilterThresholdValue { get; set; }

        /// <summary>
        ///     Gets or sets the name of the column for graph filtering.
        /// </summary>
        public string GraphFilterColumnName { get; set; }

        /// <summary>
        ///     Gets or sets the ID of the column for graph filtering.
        /// </summary>
        public Guid GraphFilterColumnGuid { get; set; }

        /// <summary>
        ///     Gets or sets an string that is used for information about the table configuration.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Gets or sets the collection of query entries that are used to highlight in this table.
        /// </summary>
        public IEnumerable<HighlightEntry> HighlightEntries
        {
            get
            {
                return this.highlightEntriesRO;
            }
            set
            {
                this.highlightEntriesRO = (value ?? EmptyArray<HighlightEntry>.Instance).ToList().AsReadOnly();
            }
        }

        /// <summary>
        ///     Gets or sets the collection of columns, in the order in which
        ///     they should be logically displayed.
        /// </summary>
        public IEnumerable<ColumnConfiguration> Columns
        {
            get
            {
                return this.columnsRO;
            }
            set
            {
                var columnCollection = (value ?? EmptyArray<ColumnConfiguration>.Instance).ToList().AsReadOnly();
                ValidateColumnCollection(columnCollection);
                this.columnsRO = columnCollection;
            }
        }

        /// <summary>
        ///     Gets the mapping of column roles to column guids, if any.
        ///     If there are no column roles, then this dictionary will
        ///     be empty.
        ///     Only the base projection of a column can be assigned a role; variants
        ///     of a column cannot be assigned a role.
        /// </summary>
        public IReadOnlyDictionary<string, Guid> ColumnRoles { get; }

        /// <summary>
        ///     Places the given column into the given column role. Only the base projection of
        ///     the column will be used; the <see cref="ColumnConfiguration.VariantGuid"/> set
        ///     on the configuration will be ignored.
        /// </summary>
        /// <param name="columnRole">
        ///     The case sensitive role into which to place the column.
        /// </param>
        /// <param name="column">
        ///     The column to place into the role.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="columnRole"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="column"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        ///     <paramref name="column"/> is a metadata column (see <see cref="AllMetadataColumns"/>).
        /// </exception>
        public void AddColumnRole(string columnRole, ColumnConfiguration column)
        {
            Guard.NotNull(column, nameof(column));

            Guard.NotNull(column.Metadata, nameof(column.Metadata));

            Guard.NotNullOrWhiteSpace(columnRole, nameof(columnRole));

            AddColumnRole(columnRole, column.Metadata.Guid);
        }

        /// <summary>
        ///     Places the given column into the given column role. Only the base
        ///     projection of the specified column will be assigned to the role.
        /// </summary>
        /// <param name="columnRole">
        ///     The case sensitive role into which to place the column.
        /// </param>
        /// <param name="columnGuid">
        ///     The guid of column to place into the role.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="columnRole"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        ///     <paramref name="columnGuid"/> is a metadata column (see <see cref="AllMetadataColumns"/>).
        /// </exception>
        public void AddColumnRole(string columnRole, Guid columnGuid)
        {
            if (AllMetadataColumns.Any(column => column.Metadata.Guid == columnGuid))
            {
                throw new InvalidOperationException($"Metadata columns may not be assigned a role.");
            }

            Guard.NotNullOrWhiteSpace(columnRole, nameof(columnRole));

            this.columnRoles[columnRole] = columnGuid;
        }

        /// <summary>
        ///     Removes the column, if any, assigned to the given <see cref="ColumnRole"/>.
        /// </summary>
        /// <param name="columnRole">
        ///     The role to be removed.
        /// </param>
        public void RemoveColumnRole(string columnRole)
        {
            this.columnRoles.Remove(columnRole);
        }

        private static void ValidateColumnCollection(
            IEnumerable<ColumnConfiguration> columns)
        {
            Debug.Assert(columns != null);

            var isLeftFreezeSeen = 0;
            var isRightFreezeSeen = 0;
            var isPivotSeen = 0;
            var isGraphSeen = 0;
            var otherColumnsSeen = 0;
            foreach (var c in columns)
            {
                if (ColumnConfigurationEqualityComparer.Default.Equals(c, PivotColumn))
                {
                    if (isGraphSeen > 0)
                    {
                        throw new InvalidOperationException("The pivot column cannot appear after the graphing column.");
                    }

                    if (isPivotSeen > 0)
                    {
                        throw new InvalidOperationException("The pivot column can only be added at most once.");
                    }

                    ++isPivotSeen;
                }
                else if (ColumnConfigurationEqualityComparer.Default.Equals(c, LeftFreezeColumn))
                {
                    if (isRightFreezeSeen > 0)
                    {
                        throw new InvalidOperationException("The left freeze column cannot appear after the right freeze column.");
                    }

                    if (isLeftFreezeSeen > 0)
                    {
                        throw new InvalidOperationException("The left freeze column can only be added at most once.");
                    }

                    ++isLeftFreezeSeen; ;
                }
                else if (ColumnConfigurationEqualityComparer.Default.Equals(c, RightFreezeColumn))
                {
                    if (isRightFreezeSeen > 0)
                    {
                        throw new InvalidOperationException("The right freeze column can only be added at most once.");
                    }

                    ++isRightFreezeSeen;
                }
                else if (ColumnConfigurationEqualityComparer.Default.Equals(c, GraphColumn))
                {
                    if (isGraphSeen > 0)
                    {
                        throw new InvalidOperationException("The graph column can only be added at most once.");
                    }

                    ++isGraphSeen;
                }
                else
                {
                    ++otherColumnsSeen;
                }
            }
        }
    }
}
