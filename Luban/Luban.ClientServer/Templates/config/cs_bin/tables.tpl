using Bright.Serialization;

{{
    name = x.name
    namespace = x.namespace
    tables = x.tables

}}

{{cs_start_name_space_grace x.namespace}} 
public partial class {{name}}
{
    {{~for table in tables ~}}
{{~if table.comment != '' ~}}
    /// <summary>
    /// {{table.escape_comment}}
    /// </summary>
{{~end~}}
    public {{table.full_name}} {{table.name}} = new();
    {{~end~}}
}

{{cs_end_name_space_grace x.namespace}}