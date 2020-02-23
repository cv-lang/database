using Cvl.Database.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cvl.Database.Base
{
    /// <summary>
    /// Obiekt przechowujący obiekty jednego typu
    /// </summary>
    public class TypeContainer
    {
        public ExecutionContext ExecutionContext { get; set; }

        /// <summary>
        /// Zwraca obiekty bazowe wyszukiwane po indeksie <see cref="ObjectBase.FullTextSearchSimpleIndex"/>
        /// </summary>
        /// <param name="filtr"></param>
        /// <returns></returns>
        public virtual IEnumerable<ObjectBase> GetBaseObjects(string filtr)
        {
            return null;
        }

        public virtual ObjectBase GetBaseObjectById(int id)
        {
            return null;
        }

        public virtual void UpdateBaseObject(ObjectBase obiekt)
        {
        }

        public virtual void AddBaseObject(ObjectBase obiekt)
        {
            
        }

        public virtual ObjectBase CreateNewBaseObject(string uzytkownikTworzacy)
        {
            throw new NotImplementedException();
        }
    }

    public class TypeContainer<T> : TypeContainer
        where T : ObjectBase, new()
    {


        public Dictionary<int, T> Objects { get; set; } = new Dictionary<int, T>();
        public int CurrentId { get; set; }

        private ExecutionContext executionContext;

        internal void Dodaj(T obiekt)
        {
            var klucz = obiekt.Id;

            if (klucz == 0)
            {
                klucz = ++CurrentId;
                obiekt.Id = klucz;
            }
            else
            {
                //nie możemy dodawać obiektów z id różnym niż 0 - bo to jest edycja
                if (Objects.Keys.Contains(klucz))
                {
                    //mamy już taki obiekt
                    throw new Exception("Istnieje już taki obiekt w bazie danych");
                }
                else
                {
                    throw new Exception("Niemożemy dodawać obiektów z id różnym od 0 - bo to jest edycja");
                }
            }

            Objects[obiekt.Id] = obiekt;
        }

        public override void AddBaseObject(ObjectBase obiekt)
        {
            Dodaj((T)obiekt);
        }

        internal void Update(T obiekt)
        {
            var staryObiekt = Objects[obiekt.Id];
            var rewizjaSerwerowa = staryObiekt.Revision;
            var rewizjaObiektu = obiekt.Revision;

            var rewizja = Math.Max(rewizjaSerwerowa, rewizjaObiektu) + 1;
            obiekt.Revision = rewizja;
            Objects[obiekt.Id] = obiekt;
        }

        public override void UpdateBaseObject(ObjectBase obiekt)
        {
            Update((T)obiekt);
        }

        public override ObjectBase CreateNewBaseObject(string uzytkownikTworzacy)
        {
            return ExecutionContext.NowyObiekt<T>(uzytkownikTworzacy);
        }

        public bool CzyJestObiekPoId(int id)
        {
            return Objects.ContainsKey(id);
        }

        public T GetObjectById(int id)
        {
            return Objects[id];
        }

        public override ObjectBase GetBaseObjectById(int id)
        {
            return Objects[id];
        }

        public IEnumerable<T> GetObjects(Func<T, bool> where = null)
        {
            var zapytanie = Objects.Values.Where(o => o.IsDeleted == false);
            if (where != null)
            {
                return zapytanie.Where(where);
            }
            return zapytanie.OrderByDescending(i => i.Id);
        }

        public override IEnumerable<ObjectBase> GetBaseObjects(string filtr)
        {
            var zapytanie = Objects.Values.Where(o => o.IsDeleted == false);

            if (!string.IsNullOrEmpty(filtr))
            {
                filtr = filtr.ToLower();
                zapytanie = zapytanie.Where(o => o.FullTextSearchSimpleIndex.ToLower().Contains(filtr));
            }


            return zapytanie.OrderByDescending(i => i.Id);
        }

        /// <summary>
        /// Return all object even makred as deleted
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public IEnumerable<T> GetAllObjects(Func<T, bool> where)
        {
            var zapytanie = Objects.Values;
            if (where != null)
            {
                return zapytanie.Where(where);
            }
            return zapytanie;
        }
    }
}
