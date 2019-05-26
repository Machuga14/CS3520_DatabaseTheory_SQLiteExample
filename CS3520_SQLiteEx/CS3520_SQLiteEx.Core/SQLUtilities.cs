using System.Data;

namespace CS3520_SQLiteEx.Core
{
  public static class SQLUtilities
  {
    /// <summary>
    /// Adds a <see cref="IDbDataParameter"/> to the provided <see cref="IDbCommand"/> object.
    /// </summary>
    /// <param name="cmd">The <see cref="IDbCommand"/> object to add a parameter to.</param>
    /// <param name="type">The type of parameter.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    public static void AddCommandParameter(this IDbCommand cmd, DbType type, string name, object value)
    {
      IDbDataParameter param = cmd.CreateParameter();
      param.DbType = type;
      param.ParameterName = name;
      param.Value = value;
      cmd.Parameters.Add(param);
    }
  }
}
