using HoneyInPacifier.Command.Model;
using System;
using System.Collections.Generic;
using System.Data;

namespace HoneyInPacifier.Language.Interface
{
    public interface IDatabaseSet : IDisposable
    {
        bool Insert(string query);

        E Insert<E>(E Entity) where E : class;

        bool Update(string query);

        bool Update<E>(E Entity, bool updateNulls) where E : class;

        bool Delete(string query);

        bool Delete<E>(E Entity) where E : class;

        long Count<E>() where E : class;

        long Count(string query);

        DataTable SelectToDatatable(string query);

        DataTable SelectToDatatable<E>() where E : class;

        DataTable SelectToDatatable<E>(int offset, int limit) where E : class;

        DataTable SelectToDatatable<E>(IEnumerable<DatabaseCommandParameter> parameters) where E : class;

        DataTable SelectToDatatable<E>(IEnumerable<DatabaseCommandParameter> parameters, int offset, int limit) where E : class;

        IEnumerable<E> Select<E>(string query) where E : class;

        IEnumerable<E> Select<E>(string query, int offset, int limit) where E : class;

        IEnumerable<E> Select<E>(IEnumerable<DatabaseCommandParameter> parameters) where E : class;

        IEnumerable<E> Select<E>(int offset, int limit) where E : class;

        IEnumerable<E> Select<E>(IEnumerable<DatabaseCommandParameter> parameters, int offset, int limit) where E : class;

        string Procedore(string query);

        Dictionary<string, object> Procedore(string procedoreName, IEnumerable<DatabaseCommandParameter> parameters);
    }
}