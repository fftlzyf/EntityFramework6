﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace FunctionalTests
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Data.Entity.Resources;
    using System.Linq;
    using ConcurrencyModel;
    using Xunit;

    public class FunctionsScenarioTests
    {
        public class ModificationFunctions
        {
            public class MetadataGeneration : TestBase
            {
                [Fact]
                public void Map_to_functions_by_convention()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder.Entity<OrderLine>().MapToStoredProcedures();

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    Assert.Equal(3, databaseMapping.Database.Functions.Count());

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .Single()
                            .ModificationFunctionMappings
                            .Single();

                    Assert.NotNull(functionMapping.InsertFunctionMapping);
                    Assert.NotNull(functionMapping.UpdateFunctionMapping);
                    Assert.NotNull(functionMapping.DeleteFunctionMapping);
                }

                [Fact]
                public void Map_to_functions_by_convention_when_complex_type()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder.Entity<Building>().MapToStoredProcedures();

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    Assert.Equal(3, databaseMapping.Database.Functions.Count());

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .Single()
                            .ModificationFunctionMappings
                            .Single();

                    Assert.NotNull(functionMapping.InsertFunctionMapping);
                    Assert.NotNull(functionMapping.UpdateFunctionMapping);
                    Assert.NotNull(functionMapping.DeleteFunctionMapping);
                }

                [Fact]
                public void Map_to_functions_by_convention_when_inheritance()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder.Entity<MigrationsCustomer>().MapToStoredProcedures();

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    Assert.Equal(9, databaseMapping.Database.Functions.Count());

                    var functionMappings
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings);

                    foreach (var functionMapping in functionMappings)
                    {
                        Assert.NotNull(functionMapping.InsertFunctionMapping);
                        Assert.NotNull(functionMapping.UpdateFunctionMapping);
                        Assert.NotNull(functionMapping.DeleteFunctionMapping);
                    }
                }

                [Fact]
                public void Map_to_functions_by_convention_when_inheritance_base_type_not_mapped_to_functions()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder.Entity<MigrationsCustomer>();
                    modelBuilder.Entity<GoldCustomer>().MapToStoredProcedures();

                    Assert.Equal(
                        Strings.BaseTypeNotMappedToFunctions(
                            typeof(MigrationsCustomer).FullName,
                            typeof(GoldCustomer).FullName),
                        Assert.Throws<InvalidOperationException>(
                            () => BuildMapping(modelBuilder)).Message);
                }

                [Fact]
                public void Map_to_functions_by_convention_when_ias()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder.Entity<Order>().MapToStoredProcedures();
                    modelBuilder.Entity<OrderLine>()
                                .MapToStoredProcedures()
                                .Ignore(ol => ol.OrderId);

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    Assert.Equal(6, databaseMapping.Database.Functions.Count());

                    var functionMappings
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings);

                    foreach (var functionMapping in functionMappings)
                    {
                        Assert.NotNull(functionMapping.InsertFunctionMapping);
                        Assert.NotNull(functionMapping.UpdateFunctionMapping);
                        Assert.NotNull(functionMapping.DeleteFunctionMapping);
                    }
                }

                [Fact]
                public void Map_to_functions_by_convention_when_many_to_many()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Tag>()
                        .HasMany(t => t.Products)
                        .WithMany(p => p.Tags)
                        .MapToStoredProcedures();

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    Assert.Equal(2, databaseMapping.Database.Functions.Count());

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .AssociationSetMappings
                            .Select(asm => asm.ModificationFunctionMapping)
                            .Single();

                    Assert.NotNull(functionMapping.InsertFunctionMapping);
                    Assert.NotNull(functionMapping.DeleteFunctionMapping);
                }

                [Fact]
                public void Map_to_functions_by_convention_when_concurrency()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder.Entity<Engine>().MapToStoredProcedures();
                    modelBuilder.Ignore<Team>();

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    Assert.Equal(3, databaseMapping.Database.Functions.Count());

                    var functionMappings
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings);

                    foreach (var functionMapping in functionMappings)
                    {
                        Assert.NotNull(functionMapping.InsertFunctionMapping);
                        Assert.NotNull(functionMapping.UpdateFunctionMapping);
                        Assert.NotNull(functionMapping.DeleteFunctionMapping);
                    }
                }
            }

            public class Apis : TestBase
            {
                [Fact]
                public void Can_configure_function_names()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<OrderLine>()
                        .MapToStoredProcedures(
                            map =>
                                {
                                    map.Insert(f => f.HasName("insert_order_line"));
                                    map.Update(f => f.HasName("update_order_line", "foo"));
                                    map.Delete(f => f.HasName("delete_order_line", "bar"));
                                });

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .Single()
                            .ModificationFunctionMappings
                            .Single();

                    Assert.Equal("insert_order_line", functionMapping.InsertFunctionMapping.Function.Name);
                    Assert.Equal("update_order_line", functionMapping.UpdateFunctionMapping.Function.Name);
                    Assert.Equal("delete_order_line", functionMapping.DeleteFunctionMapping.Function.Name);
                    Assert.Equal("foo", functionMapping.UpdateFunctionMapping.Function.Schema);
                    Assert.Equal("bar", functionMapping.DeleteFunctionMapping.Function.Schema);
                }

                [Fact]
                public void Can_configure_parameter_names()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Building>()
                        .MapToStoredProcedures(
                            map =>
                                {
                                    map.Insert(f => f.Parameter(b => b.Address.Line1, "ins_line1"));
                                    map.Update(f => f.Parameter(b => b.Id, "upd_id"));
                                    map.Delete(f => f.Parameter(b => b.Id, "del_id"));
                                });

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .Single()
                            .ModificationFunctionMappings
                            .Single();

                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "ins_line1"));
                    Assert.NotNull(functionMapping.UpdateFunctionMapping.Function.Parameters.Single(p => p.Name == "upd_id"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "del_id"));
                }

                [Fact]
                public void Can_configure_original_value_column_names_when_update()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Engine>()
                        .MapToStoredProcedures(
                            map => map.Update(
                                f =>
                                    {
                                        f.Parameter(e => e.Name, "name_cur", "name_orig");
                                        f.Parameter(e => e.StorageLocation.Latitude, "lat_cur", "lat_orig");
                                    }));
                    modelBuilder.Ignore<Team>();

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    var function
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings)
                            .Select(fm => fm.UpdateFunctionMapping.Function)
                            .Single();

                    Assert.NotNull(function.Parameters.Single(p => p.Name == "name_cur"));
                    Assert.NotNull(function.Parameters.Single(p => p.Name == "name_orig"));
                    Assert.NotNull(function.Parameters.Single(p => p.Name == "lat_cur"));
                    Assert.NotNull(function.Parameters.Single(p => p.Name == "lat_orig"));
                }

                [Fact]
                public void Configuring_original_value_for_non_concurrency_token_should_throw()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Engine>()
                        .MapToStoredProcedures(
                            map => map.Update(
                                f => f.Parameter(e => e.Id, "id", "boom")));
                    modelBuilder.Ignore<Team>();

                    Assert.Equal(
                        Strings.ModificationFunctionParameterNotFoundOriginal("Id", "Engine_Update"),
                        Assert.Throws<InvalidOperationException>(
                            () => BuildMapping(modelBuilder)).Message);
                }

                [Fact]
                public void Configuring_parameter_when_not_valid_for_operation_should_throw()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<OrderLine>()
                        .MapToStoredProcedures(
                            map => map.Delete(
                                f =>
                                    {
                                        f.HasName("del_ol");
                                        f.Parameter(e => e.IsShipped, "boom");
                                    }));

                    Assert.Equal(
                        Strings.ModificationFunctionParameterNotFound("IsShipped", "del_ol"),
                        Assert.Throws<InvalidOperationException>(
                            () => BuildMapping(modelBuilder)).Message);
                }

                [Fact]
                public void Can_configure_result_binding_column_names()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Order>()
                        .MapToStoredProcedures(
                            map =>
                                {
                                    map.Insert(f => f.Result(o => o.OrderId, "order_id"));
                                    map.Update(f => f.Result(o => o.Version, "timestamp"));
                                });

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings)
                            .Single();

                    Assert.NotNull(functionMapping.InsertFunctionMapping.ResultBindings.Single(rb => rb.ColumnName == "order_id"));
                    Assert.NotNull(functionMapping.UpdateFunctionMapping.ResultBindings.Single(rb => rb.ColumnName == "timestamp"));
                }

                [Fact]
                public void Configuring_binding_for_complex_property_should_throw()
                {
                    var modelBuilder = new DbModelBuilder();

                    Assert.Equal(
                        Strings.InvalidPropertyExpression("e => e.StorageLocation.Latitude"),
                        Assert.Throws<InvalidOperationException>(
                            () => modelBuilder
                                      .Entity<Engine>()
                                      .MapToStoredProcedures(
                                          map => map.Update(
                                              f => f.Result(e => e.StorageLocation.Latitude, "boom")))).Message);
                }

                [Fact]
                public void Configuring_binding_for_missing_property_should_throw()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Order>()
                        .MapToStoredProcedures(
                            map => map.Insert(f => f.Result(o => o.Type, "boom")));

                    Assert.Equal(
                        Strings.ResultBindingNotFound("Type", "Order_Insert"),
                        Assert.Throws<InvalidOperationException>(
                            () => BuildMapping(modelBuilder)).Message);
                }

                [Fact]
                public void Can_configure_rows_affected_column_name()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Order>()
                        .MapToStoredProcedures(
                            map =>
                                {
                                    map.Update(f => f.RowsAffectedParameter("rows_affected1"));
                                    map.Delete(f => f.RowsAffectedParameter("rows_affected2"));
                                });

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings)
                            .Single();

                    Assert.Equal("rows_affected1", functionMapping.UpdateFunctionMapping.RowsAffectedParameter.Name);
                    Assert.Equal("rows_affected2", functionMapping.DeleteFunctionMapping.RowsAffectedParameter.Name);
                }

                [Fact]
                public void Configuring_missing_rows_affected_parameter_should_throw()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<OrderLine>()
                        .MapToStoredProcedures(
                            map => map.Update(f => f.RowsAffectedParameter("rows_affected")));

                    Assert.Equal(
                        Strings.NoRowsAffectedParameter("OrderLine_Update"),
                        Assert.Throws<InvalidOperationException>(
                            () => BuildMapping(modelBuilder)).Message);
                }

                [Fact]
                public void Can_configure_many_to_many_modification_functions()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Tag>()
                        .HasMany(t => t.Products)
                        .WithMany(p => p.Tags)
                        .MapToStoredProcedures(
                            map =>
                                {
                                    map.Insert(
                                        f =>
                                            {
                                                f.HasName("ins_product_tag");
                                                f.LeftKeyParameter(t => t.Id, "tag_id");
                                                f.RightKeyParameter(p => p.Id, "product_id");
                                            });
                                    map.Delete(
                                        f =>
                                            {
                                                f.HasName("del_product_tag", "bar");
                                                f.LeftKeyParameter(t => t.Id, "tag_id");
                                                f.RightKeyParameter(p => p.Id, "product_id");
                                            });
                                });

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    Assert.Equal(2, databaseMapping.Database.Functions.Count());

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .AssociationSetMappings
                            .Select(asm => asm.ModificationFunctionMapping)
                            .Single();

                    Assert.Equal("ins_product_tag", functionMapping.InsertFunctionMapping.Function.Name);
                    Assert.Equal("del_product_tag", functionMapping.DeleteFunctionMapping.Function.Name);
                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "tag_id"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "tag_id"));
                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "product_id"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "product_id"));
                    Assert.Equal("bar", functionMapping.DeleteFunctionMapping.Function.Schema);
                }

                [Fact]
                public void Can_configure_many_to_many_modification_functions_from_both_ends()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Tag>()
                        .HasMany(t => t.Products)
                        .WithMany(p => p.Tags)
                        .MapToStoredProcedures(
                            map => map.Insert(
                                f =>
                                    {
                                        f.HasName("ins_product_tag");
                                        f.LeftKeyParameter(t => t.Id, "tag_id");
                                    }));

                    modelBuilder
                        .Entity<ProductA>()
                        .HasMany(p => p.Tags)
                        .WithMany(t => t.Products)
                        .MapToStoredProcedures(
                            map => map.Delete(
                                f =>
                                    {
                                        f.HasName("del_product_tag");
                                        f.LeftKeyParameter(p => p.Id, "product_id");
                                        f.RightKeyParameter(t => t.Id, "tag_id");
                                    }));

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    Assert.Equal(2, databaseMapping.Database.Functions.Count());

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .AssociationSetMappings
                            .Select(asm => asm.ModificationFunctionMapping)
                            .Single();

                    Assert.Equal("ins_product_tag", functionMapping.InsertFunctionMapping.Function.Name);
                    Assert.Equal("del_product_tag", functionMapping.DeleteFunctionMapping.Function.Name);
                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "tag_id"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "tag_id"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "product_id"));
                }

                [Fact]
                public void Configuring_parameter_when_not_valid_for_many_to_many_should_throw()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Tag>()
                        .HasMany(t => t.Products)
                        .WithMany(p => p.Tags)
                        .MapToStoredProcedures(
                            map => map.Insert(
                                f => f.LeftKeyParameter(t => t.Name, "tag_id")));

                    Assert.Equal(
                        Strings.ModificationFunctionParameterNotFound("Name", "Tag_Products_Insert"),
                        Assert.Throws<InvalidOperationException>(
                            () => BuildMapping(modelBuilder)).Message);
                }

                [Fact]
                public void Configuring_parameter_when_conflicting_configuration_for_many_to_many_should_throw()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Tag>()
                        .HasMany(t => t.Products)
                        .WithMany(p => p.Tags)
                        .MapToStoredProcedures(
                            map => map.Insert(f => f.HasName("ins_product_tag")));

                    modelBuilder
                        .Entity<ProductA>()
                        .HasMany(p => p.Tags)
                        .WithMany(t => t.Products)
                        .MapToStoredProcedures(
                            map => map.Insert(f => f.HasName("boom")));

                    Assert.Equal(
                        Strings.ConflictingFunctionsMapping("Tags", "FunctionalTests.ProductA"),
                        Assert.Throws<InvalidOperationException>(
                            () => BuildMapping(modelBuilder)).Message);
                }

                [Fact]
                public void Can_configure_composite_ia_fk_parameters_from_nav_prop_on_principal()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Order>()
                        .HasKey(
                            o => new
                                     {
                                         o.OrderId,
                                         o.Type
                                     });

                    modelBuilder
                        .Entity<OrderLine>()
                        .MapToStoredProcedures(
                            map =>
                                {
                                    map.Insert(
                                        f => f.Association<Order>(
                                            o => o.OrderLines,
                                            a =>
                                                {
                                                    a.Parameter(o => o.OrderId, "order_id1");
                                                    a.Parameter(o => o.Type, "the_type1");
                                                }));
                                    map.Update(
                                        f => f.Association<Order>(
                                            o => o.OrderLines,
                                            a =>
                                                {
                                                    a.Parameter(o => o.OrderId, "order_id2");
                                                    a.Parameter(o => o.Type, "the_type2");
                                                }));
                                    map.Delete(
                                        f => f.Association<Order>(
                                            o => o.OrderLines,
                                            a =>
                                                {
                                                    a.Parameter(o => o.OrderId, "order_id3");
                                                    a.Parameter(o => o.Type, "the_type3");
                                                }));
                                });

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings)
                            .Single();

                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "order_id1"));
                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "the_type1"));
                    Assert.NotNull(functionMapping.UpdateFunctionMapping.Function.Parameters.Single(p => p.Name == "order_id2"));
                    Assert.NotNull(functionMapping.UpdateFunctionMapping.Function.Parameters.Single(p => p.Name == "the_type2"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "order_id3"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "the_type3"));
                }

                [Fact]
                public void Can_configure_composite_ia_fk_parameters_from_nav_prop_on_dependent_and_last_wins()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Ignore<Chassis>()
                        .Ignore<Sponsor>()
                        .Ignore<TestDriver>()
                        .Ignore<Gearbox>()
                        .Ignore<Engine>();

                    modelBuilder
                        .Entity<Team>()
                        .HasKey(
                            o => new
                                     {
                                         o.Id,
                                         o.Name
                                     });

                    modelBuilder
                        .Entity<Driver>()
                        .MapToStoredProcedures(
                            map =>
                                {
                                    map.Insert(
                                        f =>
                                            {
                                                f.Association<Team>(
                                                    t => t.Drivers,
                                                    a =>
                                                        {
                                                            a.Parameter(t => t.Id, "team_id0");
                                                            a.Parameter(t => t.Name, "team_name0");
                                                        });
                                                f.Parameter(d => d.Team.Id, "team_id1");
                                                f.Parameter(d => d.Team.Name, "team_name1");
                                            });
                                    map.Update(
                                        f =>
                                            {
                                                f.Parameter(d => d.Team.Id, "team_id2");
                                                f.Parameter(d => d.Team.Name, "team_name2");
                                            });
                                    map.Delete(
                                        f =>
                                            {
                                                f.Parameter(d => d.Team.Id, "team_id3");
                                                f.Parameter(d => d.Team.Name, "team_name3");
                                            });
                                })
                        .Ignore(d => d.Name);

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings)
                            .Single();

                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "team_id1"));
                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "team_name1"));
                    Assert.NotNull(functionMapping.UpdateFunctionMapping.Function.Parameters.Single(p => p.Name == "team_id2"));
                    Assert.NotNull(functionMapping.UpdateFunctionMapping.Function.Parameters.Single(p => p.Name == "team_name2"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "team_id3"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "team_name3"));
                }

                [Fact]
                public void Can_configure_ia_fk_self_ref_parameters_from_nav_prop_on_dependent()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Item>()
                        .MapToStoredProcedures(
                            map =>
                                {
                                    map.Insert(f => f.Parameter(i => i.ParentItem.Id, "item_id1"));
                                    map.Update(f => f.Parameter(i => i.ParentItem.Id, "item_id2"));
                                    map.Delete(f => f.Parameter(i => i.ParentItem.Id, "item_id3"));
                                });

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings)
                            .Single();

                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "item_id1"));
                    Assert.NotNull(functionMapping.UpdateFunctionMapping.Function.Parameters.Single(p => p.Name == "item_id2"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "item_id3"));
                }

                [Fact]
                public void Can_configure_ia_fk_self_ref_parameters_from_nav_prop_on_principal()
                {
                    var modelBuilder = new DbModelBuilder();

                    modelBuilder
                        .Entity<Item>()
                        .MapToStoredProcedures(
                            map =>
                                {
                                    map.Insert(f => f.Association<Item>(o => o.ChildrenItems, a => a.Parameter(i => i.Id, "item_id1")));
                                    map.Update(
                                        f =>
                                            {
                                                f.Parameter(i => i.Id, "id2");
                                                f.Association<Item>(o => o.ChildrenItems, a => a.Parameter(i => i.Id, "item_id2"));
                                            });
                                    map.Delete(
                                        f =>
                                            {
                                                f.Association<Item>(o => o.ChildrenItems, a => a.Parameter(i => i.Id, "item_id3"));
                                                f.Parameter(i => i.Id, "id3");
                                            });
                                });

                    var databaseMapping = BuildMapping(modelBuilder);

                    databaseMapping.AssertValid();

                    var functionMapping
                        = databaseMapping
                            .EntityContainerMappings
                            .Single()
                            .EntitySetMappings
                            .SelectMany(esm => esm.ModificationFunctionMappings)
                            .Single();

                    Assert.NotNull(functionMapping.InsertFunctionMapping.Function.Parameters.Single(p => p.Name == "item_id1"));
                    Assert.NotNull(functionMapping.UpdateFunctionMapping.Function.Parameters.Single(p => p.Name == "id2"));
                    Assert.NotNull(functionMapping.UpdateFunctionMapping.Function.Parameters.Single(p => p.Name == "item_id2"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "id3"));
                    Assert.NotNull(functionMapping.DeleteFunctionMapping.Function.Parameters.Single(p => p.Name == "item_id3"));
                }
            }
        }

        public class AdvancedMapping : AdvancedMappingScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class Associations : AssociationScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class BasicMapping : BasicMappingScenarioTests
        {
            public override void Abstract_in_middle_of_hierarchy_with_TPC()
            {
                //TODO: Bug #842
            }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class ComplexTypes : ComplexTypeScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class Configuration : ConfigurationScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class Conventions : ConventionsScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class DataAnnotations : DataAnnotationScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class Enums : EnumsScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class Inheritance : InheritanceScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class PropertyConfiguration : PropertyConfigurationScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }

        public class Spatial : SpatialScenarioTests
        {
            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Entities().Configure(c => c.MapToStoredProcedures());
            }
        }
    }
}