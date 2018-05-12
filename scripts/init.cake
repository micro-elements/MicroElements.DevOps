
#load ./common.cake

// see: https://github.com/micro-elements/MicroElements.DevOps.Tutorial/blob/master/docs/01_project_structure.md
public static void CreateProjectStructure(ICakeContext context, ScriptArgs args)
{
    if(context.DirectoryExists(args.SrcDir))
        context.Information("src already exists.");
    else
    {
        context.CreateDirectory(args.SrcDir);
        context.Information("src created.");
    }

    if(context.DirectoryExists(args.TestDir))
        context.Information("test already exists.");
    {
        context.CreateDirectory(args.TestDir);
        context.Information("test created.");
    }
}

public static void CreateCommonProjectFiles(ScriptArgs args)
{
    var context = args.Context;
    var version_props_file_name = args.Root + context.File("version.props");
    if(context.FileExists(version_props_file_name))
        context.Information("version.props file already exists.");
    else
    {
        var version_props_content = ReadTemplate(args, "version.props.xml");
        System.IO.File.WriteAllText(version_props_file_name, version_props_content);
        context.Information("version.props created.");
    }
}
