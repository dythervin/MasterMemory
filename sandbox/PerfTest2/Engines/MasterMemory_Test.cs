using MasterMemory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;

namespace TestPerfLiteDB
{
    public class MasterMemory_Test : ITest
    {
        private string _filename;
        private int _count;

        readonly Database database;

        public int Count { get { return _count; } }
        public int FileLength { get { return (int)new FileInfo(_filename).Length; } }

        public MasterMemory_Test(int count)
        {
            _count = count;
            _filename = "mastermemorydatabase-" + Guid.NewGuid().ToString("n") + ".db";
            database = new Database();
        }

        public IEnumerable<TestDoc> MakeDoc()
        {
            foreach (var doc in Helper.GetDocs(_count))
            {
                var v = new TestDoc
                {
                    id = doc["_id"].AsInt32,
                    name = doc["name"].AsString,
                    lorem = doc["lorem"].AsString
                };

                yield return v;
            }
        }

        public void Insert()
        {
            database.Transaction(transaction =>
            {
                transaction.Insert(MakeDoc());
            });
            File.WriteAllBytes(_filename, MemoryPackSerializer.Serialize(database));
        }

        public void Bulk()
        {

        }

        public void CreateIndex()
        {

        }

        public void Dispose()
        {
            database.Dispose();
        }

        public void Prepare()
        {

        }

        public void Query()
        {
            for (var i = 0; i < _count; i++)
            {
                //TestDoc d;
                database.TestDocTable.GetById(i);
            }
        }

        public void Update()
        {
            database.Transaction(transaction =>
            {
                foreach (var doc in MakeDoc())
                {
                    transaction.Replace(doc);
                }
            });
        }
    }
}
