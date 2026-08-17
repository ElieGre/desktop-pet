// Procedurally draws placeholder pixel-art croc sprite sheets and writes them into
// src/DesktopPet/Assets/Sprites. Re-run (`dotnet run --project tools/GenerateSprites`)
// any time you want to regenerate the placeholders; swap the output PNGs for real art
// later as long as frame size/count match (see README).
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

const int FrameW = 32;
const int FrameH = 24;
const int Scale = 4;

var bodyDark = Color.FromArgb(255, 46, 125, 50);
var bodyLight = Color.FromArgb(255, 129, 199, 132);
var outline = Color.FromArgb(255, 27, 74, 30);
var white = Color.White;
var black = Color.Black;
var teeth = Color.FromArgb(255, 255, 253, 231);

Bitmap DrawFrame(bool eyesClosed, int legPhase)
{
    var bmp = new Bitmap(FrameW, FrameH, PixelFormat.Format32bppArgb);

    void Rect(int x, int y, int w, int h, Color c)
    {
        for (var i = x; i < x + w; i++)
            for (var j = y; j < y + h; j++)
                if (i >= 0 && i < FrameW && j >= 0 && j < FrameH)
                    bmp.SetPixel(i, j, c);
    }

    // tail (left, tapering)
    Rect(0, 14, 6, 3, bodyDark);
    Rect(1, 13, 4, 1, bodyDark);
    Rect(2, 17, 3, 1, bodyDark);

    // body
    Rect(4, 10, 18, 7, bodyDark);
    Rect(5, 15, 16, 2, bodyLight);

    // head/snout (right side)
    Rect(20, 8, 8, 5, bodyDark);
    Rect(26, 9, 4, 3, bodyDark);

    // mouth line + teeth
    Rect(21, 12, 9, 1, outline);
    Rect(23, 13, 1, 1, teeth);
    Rect(27, 13, 1, 1, teeth);

    // eye (blinks on alternate idle frame)
    if (eyesClosed)
        Rect(22, 8, 3, 1, outline);
    else
    {
        Rect(22, 7, 3, 3, white);
        Rect(23, 8, 1, 1, black);
    }

    // back ridge bumps
    Rect(7, 9, 2, 1, outline);
    Rect(11, 9, 2, 1, outline);
    Rect(15, 9, 2, 1, outline);

    // legs, cycling through 4 phases for the waddle
    int[] frontY = { 17, 18, 17, 16 };
    int[] backY = { 18, 17, 16, 17 };
    Rect(8, frontY[legPhase], 3, 2, bodyDark);
    Rect(16, backY[legPhase], 3, 2, bodyDark);

    return bmp;
}

Bitmap ScaleUp(Bitmap src)
{
    var dst = new Bitmap(src.Width * Scale, src.Height * Scale, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(dst);
    g.InterpolationMode = InterpolationMode.NearestNeighbor;
    g.PixelOffsetMode = PixelOffsetMode.Half;
    g.DrawImage(src, 0, 0, dst.Width, dst.Height);
    return dst;
}

Bitmap BuildSheet(Bitmap[] frames)
{
    var w = frames[0].Width;
    var h = frames[0].Height;
    var sheet = new Bitmap(w * frames.Length, h, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(sheet);
    for (var i = 0; i < frames.Length; i++)
        g.DrawImageUnscaled(frames[i], i * w, 0);
    return sheet;
}

string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DesktopPet.sln")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (DesktopPet.sln not found).");
}

var idleFrames = new[] { ScaleUp(DrawFrame(false, 0)), ScaleUp(DrawFrame(true, 0)) };
var walkFrames = new[]
{
    ScaleUp(DrawFrame(false, 0)),
    ScaleUp(DrawFrame(false, 1)),
    ScaleUp(DrawFrame(false, 2)),
    ScaleUp(DrawFrame(false, 3)),
};

var outDir = Path.Combine(FindRepoRoot(), "src", "DesktopPet", "Assets", "Sprites");
Directory.CreateDirectory(outDir);

BuildSheet(idleFrames).Save(Path.Combine(outDir, "croc_idle.png"), ImageFormat.Png);
BuildSheet(walkFrames).Save(Path.Combine(outDir, "croc_walk.png"), ImageFormat.Png);

Console.WriteLine($"Wrote croc_idle.png (2 frames) and croc_walk.png (4 frames) to {outDir}");
