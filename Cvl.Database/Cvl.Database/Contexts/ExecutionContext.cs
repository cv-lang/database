using Cvl.Database.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cvl.Database.Contexts
{
    public class ExecutionContext
    {
        public string UserName { get; set; }
        internal ObjectBase NowyObiekt<T>(string uzytkownikTworzacy)
            where T : ObjectBase, new()
        {
            var newObject = new T();

            newObject.CreatedDate = DateTime.Now;
            newObject.ModifiedDate = newObject.CreatedDate;
            newObject.ReadedDate = newObject.CreatedDate;

            newObject.CreatedBy = UserName;
            newObject.ModifiedBy = UserName;
            newObject.ReadedBy = UserName;

            return newObject;
        }
    }
}
