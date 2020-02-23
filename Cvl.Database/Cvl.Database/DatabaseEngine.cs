using Cvl.Database.Base;
using Cvl.Database.Contexts;
using Cvl.Database.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Cvl.Database
{
    public class DatabaseEngine
    {
        public DatabaseEngine(string nazwaBazyDanych)
        {
            NazwaBazyDanych = nazwaBazyDanych;

            //kongiguracja
            folderBazyDanych = "./";
            plikBazyDanych = folderBazyDanych + NazwaBazyDanych + ".xml";
            folderKopiBazyDanych = "./kopie/";
        }


        public Dictionary<string, TypeContainer> KonteneryTypu { get; set; } = new Dictionary<string, TypeContainer>();
        public object lockBazyDanych = new object();

        #region Konfiguracaj        

        private string folderKopiBazyDanych;
        private string folderBazyDanych;
        private string plikBazyDanych;
        public string NazwaBazyDanych;
               

        private string pobierzSciezkePlikuKopiBazyDanych()
        {
            if (Directory.Exists(folderKopiBazyDanych) == false)
            {
                throw new Exception("Brak folderu kopi bazy danych. Utrzórz go lub zmień konfiguracje. " +
                    $"Sprawdzany w ścieżce {folderKopiBazyDanych}");
            }

            var plikKopi = folderKopiBazyDanych +
                    "kopiaBazy-" + NazwaBazyDanych + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".xml";

            return plikKopi;
        }
        #endregion

        #region Zapis i odczyt bazy danych
        public void Wczytaj()
        {
            lock (lockBazyDanych)
            {
                var plik = plikBazyDanych;
                if (File.Exists(plik) == false)
                {
                    throw new Exception($"Brak pliku bazy danych. Szukam go pod sciezka '{plik}'." +
                        $" Plik jest wymagany do uruchomienia aplikacji");
                }
                else
                {
                    var xml = File.ReadAllText(plik);
                    var kontener = Serializator.DeserializeObject<Dictionary<string, TypeContainer>>(xml);
                    KonteneryTypu = kontener;
                }
            }
        }

        public void Zapisz()
        {
            lock (lockBazyDanych)
            {
                var plik = plikBazyDanych;
                if (File.Exists(plik) == false)
                {
                    throw new Exception($"Brak pliku bazy danych. Szukam go pod sciezka {plik}." +
                        $" Plik jest wymagany do uruchomienia aplikacji");
                }

                var xml = Serializator.SerializeObject(KonteneryTypu);
                File.WriteAllText(plik, xml);

                //zapisuję kopię
                var plikKopi = pobierzSciezkePlikuKopiBazyDanych();
                File.WriteAllText(plikKopi, xml);
            }
        }
        #endregion

        #region Pobieranie, dodawanie i edycja

        private TypeContainer<T> pobierzKontener<T>() where T : ObjectBase, new()
        {
            var nazwaTypu = typeof(T).FullName;
            if (KonteneryTypu.ContainsKey(nazwaTypu) == false)
            {
                KonteneryTypu[nazwaTypu] = new TypeContainer<T>();
            }
            var kontener = KonteneryTypu[nazwaTypu] as TypeContainer<T>;
            return kontener;
        }


        private TypeContainer pobierzKontener(string nazwaTypu)
        {
            if (KonteneryTypu.ContainsKey(nazwaTypu) == false)
            {
                var d1 = typeof(TypeContainer<>);
                var typInstancji = pobierzTyp(nazwaTypu);
                Type[] typeArgs = { typInstancji };
                var makeme = d1.MakeGenericType(typeArgs);
                var k = Activator.CreateInstance(makeme) as TypeContainer;
                KonteneryTypu[nazwaTypu] = k;
            }

            var kontener = KonteneryTypu[nazwaTypu] as TypeContainer;
            return kontener;
        }

        public void Dodaj<T>(ExecutionContext kontekst, T obiekt) where T : ObjectBase, new()
        {
            var kontener = pobierzKontener<T>();

            zaznaczZmodyfikowanieObiektu(kontekst, obiekt);

            lock (lockBazyDanych)
                lock (kontener)
                {
                    kontener.Dodaj(obiekt);
                }
        }

        public void Dodaj(ExecutionContext kontekst, ObjectBase obiekt, string nazwaTypu)
        {
            var kontener = pobierzKontener(nazwaTypu);

            zaznaczZmodyfikowanieObiektu(kontekst, obiekt);

            lock (lockBazyDanych)
                lock (kontener)
                {
                    kontener.AddBaseObject(obiekt);
                }
        }

        public void Edytuj<T>(ExecutionContext kontekst, T obiekt) where T : ObjectBase, new()
        {
            var kontener = pobierzKontener<T>();

            zaznaczZmodyfikowanieObiektu(kontekst, obiekt);

            lock (lockBazyDanych)
                lock (kontener)
                {
                    kontener.Update(obiekt);
                }
        }

        public void Edytuj(ExecutionContext kontekst, ObjectBase obiekt, string nazwaTypu)
        {
            var kontener = pobierzKontener(nazwaTypu);

            zaznaczZmodyfikowanieObiektu(kontekst, obiekt);

            lock (lockBazyDanych)
                lock (kontener)
                {
                    kontener.UpdateBaseObject(obiekt);
                }
        }

        public ObjectBase NowyObiekt(ExecutionContext kontekst, string nazwaTypu)
        {
            var kontener = pobierzKontener(nazwaTypu);

            lock (lockBazyDanych)
                lock (kontener)
                {
                    return kontener.CreateNewBaseObject(kontekst.UserName);
                }
        }

        private void zaznaczZmodyfikowanieObiektu(ExecutionContext kontekst, ObjectBase obiekt)
        {
            obiekt.ModifiedBy = kontekst.UserName;
            obiekt.ModifiedDate = DateTime.Now;
        }

        public bool CzyJestObiekPoId<T>(ExecutionContext kontekst, int id) where T : ObjectBase, new()
        {
            var kontener = pobierzKontener<T>();

            lock (lockBazyDanych)
                lock (kontener)
                {
                    return kontener.CzyJestObiekPoId(id);
                }
        }

        public T PobierzObiektPoId<T>(ExecutionContext kontekst, int id) where T : ObjectBase, new()
        {
            var kontener = pobierzKontener<T>();

            lock (lockBazyDanych)
                lock (kontener)
                {
                    return kontener.GetObjectById(id);
                }
        }

        public ObjectBase PobierzObiektPoId(ExecutionContext kontekst, int id, string nazwaTypu)
        {
            var kontener = pobierzKontener(nazwaTypu);

            lock (lockBazyDanych)
                lock (kontener)
                {
                    return kontener.GetBaseObjectById(id);
                }
        }

        public IEnumerable<T> PobierzObiekty<T>(ExecutionContext kontekst, Func<T, bool> where = null) where T : ObjectBase, new()
        {
            var kontener = pobierzKontener<T>();

            lock (lockBazyDanych)
                lock (kontener)
                {
                    return kontener.GetObjects(where);
                }
        }

        public IEnumerable<ObjectBase> PobierzObiekty(ExecutionContext kontekst, string nazwaTypu, string filtr = null)
        {
            var kontener = pobierzKontener(nazwaTypu);
            return kontener.GetBaseObjects(filtr);
        }

        public IEnumerable<T> PobierzWszystkieObiekty<T>(ExecutionContext kontekst, Func<T, bool> where = null) where T : ObjectBase, new()
        {
            var kontener = pobierzKontener<T>();

            lock (lockBazyDanych)
                lock (kontener)
                {
                    return kontener.GetAllObjects(where);
                }
        }

        #endregion

        #region tworzenie typów z tekstu i rejestrowanie assemblies

        private static List<string> listaBibliotek = new List<string>();

        public static void ZarejestrujBiblioteke(Assembly assembly)
        {
            listaBibliotek.Add(assembly.FullName);
        }

        /// <summary>
        /// Zwraca typ na podstawie pełnej nazwy typów - odpowiednie assembly zgaduje na podstawie listy zarejestowaneych
        /// </summary>
        /// <param name="nazwaTypu"></param>
        /// <returns></returns>
        private Type pobierzTyp(string nazwaTypu)
        {
            foreach (var biblioteka in listaBibliotek)
            {
                var typ = Type.GetType(nazwaTypu + ", " + biblioteka);
                if (typ != null)
                {
                    return typ;
                }
            }
            throw new Exception("Brak typu " + nazwaTypu);
        }

        #endregion

        #region Doczytywanie

        //public void DoczytajObiekty(ExecutionContext kontekst, ObjectBase obiekt)
        //{
        //    var propercje = obiekt.GetType().GetProperties();
        //    foreach (var item in propercje)
        //    {
        //        var f = item.GetCustomAttributes(typeof(ForeignKeyAttribute), false)
        //            .FirstOrDefault() as ForeignKeyAttribute;

        //        if (f != null)
        //        {
        //            var pid = obiekt.GetType().GetProperty(f.ProperyId);
        //            var id = pid.GetValue(obiekt) as int?;
        //            if (id.HasValue)
        //            {
        //                //pobieramy obiekt po id
        //                var kontener = pobierzKontener(item.PropertyType.FullName);
        //                var obiektDoczytany = kontener.PobierzPoIdBazowe(id.Value);
        //                item.SetValue(obiekt, obiektDoczytany);
        //            }
        //        }

        //    }
        //}

        #endregion
    }
}
