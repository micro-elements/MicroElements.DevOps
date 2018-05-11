
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
