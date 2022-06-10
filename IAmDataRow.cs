using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeatForward
{
    public interface IAmDataRow<TSelf> where TSelf : IAmDataRow<TSelf>
    {

        public TSelf fillFromCurrentRow(Microsoft.Data.Sqlite.SqliteDataReader r);

        public Dictionary<string, (bool danger, object val)> postValues();
    }
}
