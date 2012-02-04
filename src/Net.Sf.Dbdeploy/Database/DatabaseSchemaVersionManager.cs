using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Net.Sf.Dbdeploy.Exceptions;
using Net.Sf.Dbdeploy.Scripts;

namespace Net.Sf.Dbdeploy.Database
{
    public class DatabaseSchemaVersionManager : IAppliedChangesProvider
    {
        private readonly QueryExecuter queryExecuter;

        private readonly string changeLogTableName;

        private readonly IDbmsSyntax syntax;

        public DatabaseSchemaVersionManager(QueryExecuter queryExecuter, IDbmsSyntax syntax, string changeLogTableName)
        {
            this.syntax = syntax;
            this.queryExecuter = queryExecuter;
            this.changeLogTableName = changeLogTableName;
        }

    	public virtual ICollection<int> GetAppliedChanges()
        {
            List<int> changeNumbers = new List<int>();
            try
            {
                string sql = string.Format("SELECT change_number FROM {0} ORDER BY change_number", this.changeLogTableName);
                
                using (IDataReader reader = this.queryExecuter.ExecuteQuery(sql))
                {
                    while (reader.Read())
                    {
                        int changeNumber = Int32.Parse(reader.GetValue(0).ToString());

						changeNumbers.Add(changeNumber);
                    }
                }

                return changeNumbers;
            }
            catch (DbException e)
            {
                throw new SchemaVersionTrackingException(
                    "Could not retrieve change log from database because: " + e.Message, e);
            }            
        }

        public string GetChangelogDeleteSql(ChangeScript script)
        {
            return string.Format("DELETE FROM {0} WHERE change_number = {1}", this.changeLogTableName, script.GetId());
        }

        public void RecordScriptApplied(ChangeScript script)
        {
            try
            {
                string sql = string.Format(
                    "INSERT INTO {0} (change_number, complete_dt, applied_by, description) VALUES ($1, {1}, {2}, $2)", 
                    this.changeLogTableName,
                    this.syntax.GenerateTimestamp(),
                    this.syntax.GenerateUser());

                this.queryExecuter.Execute(
                        sql,
                        script.GetId(),
                        script.GetDescription());
            }
            catch (DbException e)
            {
                throw new SchemaVersionTrackingException("Could not update change log because: " + e.Message, e);
            }
        }
    }
}
