using MasterMemory;
using System.Linq;
using MessagePack;
using System;
using System.IO;
using System.Buffers;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Globalization;
using MasterMemory.Annotations;
using MasterMemory.Validation;

// IValidatable����������ƌ��ؑΏۂɂȂ�
[Table("quest_master"), MessagePackObject(true)]
public class Quest
{
    // UniqueKey�̏ꍇ��Validate���Ƀf�t�H���g�ŏd�����̌��؂������
    [PrimaryKey]
    public int Id { get; set; }
    public string Name { get; set; }
    
    [SecondaryKey]
    public int RewardId { get; set; }
    public int Cost { get; set; }
    public MyEnum MyProperty { get; set; }

    public enum MyEnum
    {
        A, B, C
    }
}

[Validator]
internal partial class QuestValidator
{
    public void Validate(IDatabase db, IValidator validator)
    {
        // �O���L�[�I�ɎQ�Ƃ������R���N�V���������o����
        var items = db.ItemTable;
        var quests = db.QuestmasterTable;

        // RewardId��0�ȏ�̂Ƃ�(0�͕�V�i�V�̂��߂̓��ʂȃt���O�Ƃ��邽�ߓ��͂����e����)
        validator.Exists(quests.GetAllSortedByRewardId(), items.GetAllSorted(), x => x > 0);

        // �R�X�g��10..20�łȂ���Ό��؃G���[
        validator.Validate(quests, x => x.Cost >= 10);
        validator.Validate(quests, x => x.Cost <= 20);

        // �ȉ��ň͂��������͈�x�����Ă΂�Ȃ����߁A�f�[�^�Z�b�g�S�̂̌��؂����������Ɏg����
        // �C���f�b�N�X�����������̈ȊO�̃��j�[�N�ǂ����̌���(0�͏d�����邽�ߏ����Ă���)
        validator.Unique(quests, x => x.RewardId, x => x != 0);
    }
}

[Table("item"), MessagePackObject(true)]
public class Item
{
    [PrimaryKey]
    public int ItemId { get; set; }
}

namespace ConsoleApp
{
    public sealed partial class MonsterTable
    {
        /* readonly */
        int maxHp;

        partial void OnAfterConstruct()
        {
            maxHp = GetAll().Max(x => x.MaxHp);
        }
    }
}

namespace ConsoleApp
{
    [Table("monster"), MessagePackObject(true)]
    public class Monster
    {
        [PrimaryKey]
        public int MonsterId { get; private set; }
        public string Name { get; private set; }
        public int MaxHp { get; private set; }

        public Monster(int MonsterId, string Name, int MaxHp)
        {
            this.MonsterId = MonsterId;
            this.Name = Name;
            this.MaxHp = MaxHp;
        }
    }

    [Table("enumkeytable"), MessagePackObject(true)]
    public class EnumKeyTable
    {
        [PrimaryKey]
        public Gender Gender { get; set; }
    }

    public enum Gender
    {
        Male, Female
    }

    [Table("person"), MessagePackObject(true)]
    public class Person
    {
        [PrimaryKey(keyOrder: 1)]
        public int PersonId { get; set; }
        [SecondaryKey(0)]
        [SecondaryKey(2, keyOrder: 1)]
        public int Age { get; set; }
        [SecondaryKey(1)]
        [SecondaryKey(2, keyOrder: 0)]
        public Gender Gender { get; set; }
        public string Name { get; set; }

        public Person()
        {
        }

        public Person(int PersonId, int Age, Gender Gender, string Name)
        {
            this.PersonId = PersonId;
            this.Age = Age;
            this.Gender = Gender;
            this.Name = Name;
        }

        public override string ToString()
        {
            return $"{PersonId} {Age} {Gender} {Name}";
        }
    }





    class ByteBufferWriter : IBufferWriter<byte>
    {
        byte[] buffer;
        int index;

        public int CurrentOffset => index;
        public ReadOnlySpan<byte> WrittenSpan => buffer.AsSpan(0, index);
        public ReadOnlyMemory<byte> WrittenMemory => new ReadOnlyMemory<byte>(buffer, 0, index);

        public ByteBufferWriter()
        {
            buffer = new byte[1024];
            index = 0;
        }

        public void Advance(int count)
        {
            index += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
        AGAIN:
            var nextSize = index + sizeHint;
            if (buffer.Length < nextSize)
            {
                Array.Resize(ref buffer, Math.Max(buffer.Length * 2, nextSize));
            }

            if (sizeHint == 0)
            {
                var result = new Memory<byte>(buffer, index, buffer.Length - index);
                if (result.Length == 0)
                {
                    sizeHint = 1024;
                    goto AGAIN;
                }
                return result;
            }
            else
            {
                return new Memory<byte>(buffer, index, sizeHint);
            }
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            return GetMemory(sizeHint).Span;
        }
    }

    [Table(nameof(Test1))]
    public class Test1
    {
        [PrimaryKey]
        public int Id { get; set; }
    }

    [MessagePackObject(false)]
    [Table(nameof(Test2))]
    public class Test2
    {
        [PrimaryKey]
        public int Id { get; set; }
    }



    class Program
    {
        static void Main(string[] args)
        {
            var csv = @"monster_id,name,max_hp
    1,foo,100
    2,bar,200";
            var fileName = "monster";

            var builder = new DatabaseBuilder();

            var meta = Database.GetMetaDatabase();
            var table = meta.GetTableInfo(fileName);

            var tableData = new List<object>();

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv)))
            using (var sr = new StreamReader(ms, Encoding.UTF8))
            using (var reader = new TinyCsvReader(sr))
            {
                while ((reader.ReadValuesWithHeader() is Dictionary<string, string> values))
                {
                    // create data without call constructor
                    var data = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(table.DataType);
    
                    foreach (var prop in table.Properties)
                    {
                        if (values.TryGetValue(prop.NameSnakeCase, out var rawValue))
                        {
                            var value = ParseValue(prop.PropertyInfo.PropertyType, rawValue);
                            if (prop.PropertyInfo.SetMethod == null)
                            {
                                throw new Exception("Target property does not exists set method. If you use {get;}, please change to { get; private set; }, Type:" + prop.PropertyInfo.DeclaringType + " Prop:" + prop.PropertyInfo.Name);
                            }
                            prop.PropertyInfo.SetValue(data, value);
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Not found \"{prop.NameSnakeCase}\" in \"{fileName}.csv\" header.");
                        }
                    }
    
                    tableData.Add(data);
                }
            }
    
            // add dynamic collection.
            builder.AppendDynamic(table.DataType, tableData);
    
            var database = builder.Build();
            database.Transaction.MaxDegreeOfParallelism = Environment.ProcessorCount;
        }

        static object ParseValue(Type type, string rawValue)
        {
            if (type == typeof(string)) return rawValue;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrWhiteSpace(rawValue)) return null;
                return ParseValue(type.GenericTypeArguments[0], rawValue);
            }

            if (type.IsEnum)
            {
                var value = Enum.Parse(type, rawValue);
                return value;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    // True/False or 0,1
                    if (int.TryParse(rawValue, out var intBool))
                    {
                        return Convert.ToBoolean(intBool);
                    }
                    return Boolean.Parse(rawValue);
                case TypeCode.Char:
                    return Char.Parse(rawValue);
                case TypeCode.SByte:
                    return SByte.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Byte:
                    return Byte.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Int16:
                    return Int16.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.UInt16:
                    return UInt16.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Int32:
                    return Int32.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.UInt32:
                    return UInt32.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Int64:
                    return Int64.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.UInt64:
                    return UInt64.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Single:
                    return Single.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Double:
                    return Double.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.Decimal:
                    return Decimal.Parse(rawValue, CultureInfo.InvariantCulture);
                case TypeCode.DateTime:
                    return DateTime.Parse(rawValue, CultureInfo.InvariantCulture);
                default:
                    if (type == typeof(DateTimeOffset))
                    {
                        return DateTimeOffset.Parse(rawValue, CultureInfo.InvariantCulture);
                    }
                    else if (type == typeof(TimeSpan))
                    {
                        return TimeSpan.Parse(rawValue, CultureInfo.InvariantCulture);
                    }
                    else if (type == typeof(Guid))
                    {
                        return Guid.Parse(rawValue);
                    }

                    // or other your custom parsing.
                    throw new NotSupportedException();
            }
        }

        // Non string escape, tiny reader with header.
        public class TinyCsvReader : IDisposable
        {
            static char[] trim = new[] { ' ', '\t' };

            readonly StreamReader reader;
            public IReadOnlyList<string> Header { get; private set; }

            public TinyCsvReader(StreamReader reader)
            {
                this.reader = reader;
                {
                    var line = reader.ReadLine();
                    if (line == null) throw new InvalidOperationException("Header is null.");

                    var index = 0;
                    var header = new List<string>();
                    while (index < line.Length)
                    {
                        var s = GetValue(line, ref index);
                        if (s.Length == 0) break;
                        header.Add(s);
                    }
                    this.Header = header;
                }
            }

            string GetValue(string line, ref int i)
            {
                var temp = new char[line.Length - i];
                var j = 0;
                for (; i < line.Length; i++)
                {
                    if (line[i] == ',')
                    {
                        i += 1;
                        break;
                    }
                    temp[j++] = line[i];
                }

                return new string(temp, 0, j).Trim(trim);
            }

            public string[] ReadValues()
            {
                var line = reader.ReadLine();
                if (line == null) return null;
                if (string.IsNullOrWhiteSpace(line)) return null;

                var values = new string[Header.Count];
                var lineIndex = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    var s = GetValue(line, ref lineIndex);
                    values[i] = s;
                }
                return values;
            }

            public Dictionary<string, string> ReadValuesWithHeader()
            {
                var values = ReadValues();
                if (values == null) return null;

                var dict = new Dictionary<string, string>();
                for (int i = 0; i < values.Length; i++)
                {
                    dict.Add(Header[i], values[i]);
                }

                return dict;
            }

            public void Dispose()
            {
                reader.Dispose();
            }
        }
    }


}


