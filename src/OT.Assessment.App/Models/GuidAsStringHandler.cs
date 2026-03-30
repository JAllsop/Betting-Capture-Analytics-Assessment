namespace OT.Assessment.App.Models
{
    public class GuidAsStringHandler : Dapper.SqlMapper.TypeHandler<string>
    {
        public override void SetValue(System.Data.IDbDataParameter parameter, string value)
        {
            parameter.Value = string.IsNullOrEmpty(value) ? DBNull.Value : new Guid(value);
        }

        public override string Parse(object value) => value?.ToString() ?? string.Empty;
    }
}