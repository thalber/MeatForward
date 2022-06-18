using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeatForward
{
    /// <summary>
    /// Convertible from/to SQL row 
    /// </summary>
    /// <typeparam name="TSelf">Impl-er itself</typeparam>
    public interface IAmDataRow<TSelf> where TSelf : IAmDataRow<TSelf>
    {
        /// <summary>
        /// populates <see cref="IAmDataRow{TSelf}"/> from current row of an <see cref="Microsoft.Data.Sqlite.SqliteDataReader"/>
        /// </summary>
        /// <param name="r"></param>
        /// <returns>itself</returns>
        public TSelf fillFromCurrentRow(Microsoft.Data.Sqlite.SqliteDataReader r);
        /// <summary>
        /// Produces data for 
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, (bool danger, object? val)> postValues();
    }
}
