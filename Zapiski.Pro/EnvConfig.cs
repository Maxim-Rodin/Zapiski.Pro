namespace Zapisi.Pro
{
    internal static class EnvConfig
    {
        public static void Load(string envPath)
        {
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }
        }
    }
}
