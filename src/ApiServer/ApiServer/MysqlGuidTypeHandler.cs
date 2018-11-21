using System;
using System.Data;

namespace WomPlatform.Web.Api {

    public class MySqlGuidTypeHandler : Dapper.SqlMapper.TypeHandler<Guid> {
        public override void SetValue(IDbDataParameter parameter, Guid guid) {
            parameter.Value = guid.ToString("N");
        }

        public override Guid Parse(object value) {
            return Guid.Parse((string)value);
        }
    }

}
