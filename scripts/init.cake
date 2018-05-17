
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

    var common_props_file_name = args.Root + context.File("common.props");
    if(context.FileExists(common_props_file_name))
        context.Information("common.props file already exists.");
    else
    {
        var common_props_content = ReadTemplate(args, "common.props.xml");
        // common props filling
        FillProjectAttributes(args);
        common_props_content = FillTags(common_props_content, args);

        System.IO.File.WriteAllText(common_props_file_name, common_props_content);
        context.Information("common.props created.");
    }

    context.Information("Adding common.props import...");
    context.Information("Processing files: "+args.SrcDir.Path + "/**/*.csproj");
    var projectFiles = context.GetFiles(args.SrcDir.Path + "/**/*.csproj");
    foreach(var projectFile in projectFiles)
    {
        context.Information($"Processing file: {projectFile}");
        var lines = System.IO.File.ReadAllLines(projectFile.FullPath).ToList();
        var import = @"<Import Project=""..\..\common.props""/>";
        var containsImport = lines.FirstOrDefault(line=>line.Contains(import))!=null;
        if(!containsImport)
        {
            lines.Insert(1, $"  {import}");
            System.IO.File.WriteAllLines(projectFile.FullPath, lines);
            context.Information($"Project: {projectFile}; Import inserted: {import}");
        }
        else
            context.Information($"Project: {projectFile}; Import already exists: {import}");
    }
}

public static void FillProjectAttributes(ScriptArgs args)
{
    /*
    <Product></Product>
    <Copyright></Copyright>
    <Authors></Authors>
    <PackageIconUrl></PackageIconUrl>
    <PackageProjectUrl></PackageProjectUrl>
    <PackageLicenseUrl></PackageLicenseUrl>
    <RepositoryType></RepositoryType>
    <RepositoryUrl></RepositoryUrl>
    */

    var result = ProcessUtils.StartProcessAndReturnOutput(args.Context, "git", "remote get-url origin");
    if(result.ExitCode==0)
    {
        args.Params.Add("RepositoryType", "git");
        //Ex: https://github.com/micro-elements/MicroElements.DevOps
        var repoUrl = result.Output.TrimEnd();
        args.Params.Add("RepositoryUrl", repoUrl);
        args.Params.Add("PackageProjectUrl", repoUrl);

        var segments = repoUrl.Split('/');
        var serverIndex = Array.IndexOf(segments, "github.com");
        if(serverIndex>0&&serverIndex<segments.Length-2)
        {
            var userName = segments[serverIndex+1];
            args.Params["userName"] = userName;

            var projectName = segments[serverIndex+2];
            if(!args.Params.ContainsKey("projectName"))
                args.Params["projectName"] = projectName;

            //<PackageLicenseUrl>https://raw.githubusercontent.com/micro-elements/MicroElements.DevOps/master/LICENSE</PackageLicenseUrl>
            args.Params["PackageLicenseUrl"] = $"https://raw.githubusercontent.com/{userName}/{projectName}/master/LICENSE";
        }

        if(args.Params.ContainsKey("projectName"))
        {
            args.Params["Product"] = args.Params["projectName"];
            args.Params["Copyright"] = $"{DateTime.Today.Year}";
        }

        //Authors
        if(args.Params.ContainsKey("userName"))
        {
            args.Params["Authors"] = args.Params["userName"];
        }
    }

    foreach (var param in args.Params)
    {
        args.Context.Information($"{param.Key}: {param.Value}");
    }
    
}

public static string FillTags(string inputXml, ScriptArgs args)
{
    foreach (var key in args.Params.Keys)
    {
        inputXml = inputXml.Replace($"<{key}></{key}>", $"<{key}>{args.Params[key]}</{key}>");
    }
    return inputXml;
}
