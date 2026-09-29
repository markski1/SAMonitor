using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace SAMonitor.Tests;

internal sealed record RecordedCommand(string Sql, Dictionary<string, object?> Parameters, DbTransaction? Transaction);

internal sealed class DbConnectionStub(Func<RecordedCommand, object?> execute) : DbConnection
{
    internal List<RecordedCommand> Commands { get; } = [];
    internal List<DbTransactionStub> Transactions { get; } = [];
    [AllowNull]
    public override string ConnectionString { get; set; } = "";
    public override string Database => "Tests";
    public override string DataSource => "Tests";
    public override string ServerVersion => "1";
    public override ConnectionState State => ConnectionState.Open;
    public override void Open() { }
    public override void Close() { }
    public override void ChangeDatabase(string databaseName) { }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        var transaction = new DbTransactionStub(this, isolationLevel);
        Transactions.Add(transaction);
        return transaction;
    }

    protected override DbCommand CreateDbCommand() => new DbCommandStub(this);

    internal object? Execute(DbCommand command)
    {
        var parameters = command.Parameters.Cast<DbParameter>().ToDictionary(
            x => x.ParameterName.TrimStart('@'), x => x.Value, StringComparer.OrdinalIgnoreCase);
        var recorded = new RecordedCommand(command.CommandText, parameters, command.Transaction);
        Commands.Add(recorded);
        return execute(recorded);
    }
}

internal sealed class DbTransactionStub(DbConnectionStub connection, IsolationLevel isolationLevel) : DbTransaction
{
    internal bool Committed { get; private set; }
    internal bool RolledBack { get; private set; }
    public override IsolationLevel IsolationLevel => isolationLevel;
    protected override DbConnection DbConnection => connection;
    public override void Commit() => Committed = true;
    public override void Rollback() => RolledBack = true;
}

internal sealed class DbCommandStub(DbConnectionStub connection) : DbCommand
{
    private readonly DbParameterCollectionStub _parameters = new();
    [AllowNull]
    public override string CommandText { get; set; } = "";
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; } = connection;
    protected override DbTransaction? DbTransaction { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    public override void Cancel() { }
    public override void Prepare() { }
    public override int ExecuteNonQuery() => Convert.ToInt32(connection.Execute(this));
    public override object? ExecuteScalar() => connection.Execute(this);
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => (DbDataReader)connection.Execute(this)!;
    protected override DbParameter CreateDbParameter() => new DbParameterStub();
}

internal sealed class DbParameterStub : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    [AllowNull]
    public override string ParameterName { get; set; } = "";
    [AllowNull]
    public override string SourceColumn { get; set; } = "";
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }
    public override void ResetDbType() { }
}

internal sealed class DbParameterCollectionStub : DbParameterCollection
{
    private readonly List<DbParameter> _parameters = [];
    public override int Count => _parameters.Count;
    public override object SyncRoot => this;
    public override int Add(object value)
    {
        _parameters.Add((DbParameter)value);
        return _parameters.Count - 1;
    }
    public override void AddRange(Array values)
    {
        foreach (var value in values) Add(value!);
    }
    public override void Clear() => _parameters.Clear();
    public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
    public override bool Contains(string value) => IndexOf(value) >= 0;
    public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
    public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();
    public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
    public override int IndexOf(string parameterName) => _parameters.FindIndex(x => x.ParameterName == parameterName);
    public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
    public override void Remove(object value) => _parameters.Remove((DbParameter)value);
    public override void RemoveAt(int index) => _parameters.RemoveAt(index);
    public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));
    protected override DbParameter GetParameter(int index) => _parameters[index];
    protected override DbParameter GetParameter(string parameterName) => GetParameter(IndexOf(parameterName));
    protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value)
    {
        int index = IndexOf(parameterName);
        if (index < 0) Add(value);
        else SetParameter(index, value);
    }
}
