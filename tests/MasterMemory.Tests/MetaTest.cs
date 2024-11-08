using FluentAssertions;
using System.Collections.Generic;
using MasterMemory.Meta;
using Xunit;

namespace MasterMemory.Tests
{
    public class MetaTest
    {
        public static MetaTable CreateMetaTable()
        {
            return new MetaTable(typeof(Sample),
                typeof(SampleTable),
                "s_a_m_p_l_e",
                new MetaProperty[]
                {
                    new MetaProperty(
                        typeof(Sample).GetProperty(
                            nameof(Sample.Id))),
                    new MetaProperty(
                        typeof(Sample).GetProperty(
                            nameof(Sample.FirstName))),
                    new MetaProperty(
                        typeof(Sample).GetProperty(
                            nameof(Sample.LastName))),
                    new MetaProperty(
                        typeof(Sample).GetProperty(
                            nameof(Sample.Age)))
                },
                new MetaIndex[]
                {
                    new MetaIndex(new System.Reflection.PropertyInfo[]
                        {
                            typeof(Sample).GetProperty(
                                nameof(Sample.Id))
                        },
                        true,
                        true,
                        Comparer<(int key, int primaryKey)>.Default),
                    new MetaIndex(new System.Reflection.PropertyInfo[]
                        {
                            typeof(Sample).GetProperty(
                                nameof(Sample.Id)),
                            typeof(Sample).GetProperty(
                                nameof(Sample.FirstName)),
                            typeof(Sample).GetProperty(
                                nameof(Sample.LastName)),
                            typeof(Sample).GetProperty(
                                nameof(Sample.Age))
                        },
                        false,
                        false,
                        Comparer<((int Id, string FirstName, string LastName, int Age) key, int primaryKey)>.Default),
                    new MetaIndex(new System.Reflection.PropertyInfo[]
                        {
                            typeof(Sample).GetProperty(
                                nameof(Sample.Id)),
                            typeof(Sample).GetProperty(
                                nameof(Sample.Age))
                        },
                        false,
                        false,
                        Comparer<((int Id, int Age) key, int primaryKey)>.Default),
                    new MetaIndex(new System.Reflection.PropertyInfo[]
                        {
                            typeof(Sample).GetProperty(
                                nameof(Sample.Id)),
                            typeof(Sample).GetProperty(
                                nameof(Sample.FirstName)),
                            typeof(Sample).GetProperty(
                                nameof(Sample.Age))
                        },
                        false,
                        false,
                        Comparer<((int Id, string FirstName, int Age) key, int primaryKey)>.Default),
                    new MetaIndex(new System.Reflection.PropertyInfo[]
                        {
                            typeof(Sample).GetProperty(
                                nameof(Sample.FirstName))
                        },
                        false,
                        false,
                        Comparer<(string key, int primaryKey)>.Default),
                    new MetaIndex(new System.Reflection.PropertyInfo[]
                        {
                            typeof(Sample).GetProperty(
                                nameof(Sample.LastName))
                        },
                        false,
                        false,
                        Comparer<(string key, int primaryKey)>.Default),
                    new MetaIndex(new System.Reflection.PropertyInfo[]
                        {
                            typeof(Sample).GetProperty(
                                nameof(Sample.Age))
                        },
                        false,
                        false,
                        Comparer<(int key, int primaryKey)>.Default),
                    new MetaIndex(new System.Reflection.PropertyInfo[]
                        {
                            typeof(Sample).GetProperty(
                                nameof(Sample.FirstName)),
                            typeof(Sample).GetProperty(
                                nameof(Sample.LastName))
                        },
                        false,
                        true,
                        Comparer<((string FirstName, string LastName) key, int primaryKey)>.Default),
                    new MetaIndex(new System.Reflection.PropertyInfo[]
                        {
                            typeof(Sample).GetProperty(
                                nameof(Sample.FirstName)),
                            typeof(Sample).GetProperty(
                                nameof(Sample.Age))
                        },
                        false,
                        false,
                        Comparer<((string FirstName, int Age) key, int primaryKey)>.Default)
                });
        }

        [Fact]
        public void Meta()
        {
            var metaDb = Database.GetMetaDatabase();

            var sampleTable = metaDb.GetTableInfo("s_a_m_p_l_e");

            sampleTable.TableName.Should().Be("s_a_m_p_l_e");

            sampleTable.Properties[0].Name.Should().Be("Id");
            sampleTable.Properties[0].NameLowerCamel.Should().Be("id");
            sampleTable.Properties[0].NameSnakeCase.Should().Be("id");

            sampleTable.Properties[2].Name.Should().Be("FirstName");
            sampleTable.Properties[2].NameLowerCamel.Should().Be("firstName");
            sampleTable.Properties[2].NameSnakeCase.Should().Be("first_name");

            var primary = sampleTable.Indexes[0];
            primary.IsUnique.Should().BeTrue();
            primary.IndexProperties[0].Name.Should().Be("Id");
        }
    }
}