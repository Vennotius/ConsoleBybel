using LeesBybel;

const string Usage = "Gebruik: lees-bybel [boek]   byvoorbeeld: lees-bybel psa";

var code = "GEN";
foreach (var arg in args)
{
    if (arg.StartsWith('-')) return Fail(Usage);
    code = arg.ToUpperInvariant();
}

var book = Book.Load(code);
if (book is null) return Fail($"Geen boek \"{code.ToLowerInvariant()}\" nie. {Usage}");
if (Console.IsInputRedirected || Console.IsOutputRedirected) return Fail("lees-bybel het ’n terminaal nodig.");

Console.TreatControlCAsInput = true;
var screen = new Screen();
screen.Enter();
Exception? failed = null;
try
{
    new App(screen, code, book).Run();
}
catch (Exception e)
{
    failed = e;     // caught, so the terminal is restored before the error shows
}
screen.Leave();
return failed is null ? 0 : Fail(failed.ToString());

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
