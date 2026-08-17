using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopPet.Pet;

/// <summary>
/// Slices a horizontal sprite sheet into frames and cycles through them. ticksPerFrame
/// lets slow animations (e.g. an occasional blink) and fast ones (leg cycles) share the
/// same DispatcherTimer without needing separate timers.
/// </summary>
public sealed class SpriteAnimator
{
    private readonly BitmapSource _sheet;
    private readonly int _frameCount;
    private readonly int _frameWidth;
    private readonly int _ticksPerFrame;
    private int _frameIndex;
    private int _tickCounter;

    public SpriteAnimator(Uri sheetUri, int frameCount, int ticksPerFrame = 1)
    {
        _sheet = new BitmapImage(sheetUri);
        _frameCount = frameCount;
        _frameWidth = _sheet.PixelWidth / frameCount;
        _ticksPerFrame = Math.Max(1, ticksPerFrame);
    }

    public ImageSource CurrentFrame =>
        new CroppedBitmap(_sheet, new Int32Rect(_frameIndex * _frameWidth, 0, _frameWidth, _sheet.PixelHeight));

    public void Reset()
    {
        _frameIndex = 0;
        _tickCounter = 0;
    }

    public void Advance()
    {
        _tickCounter++;
        if (_tickCounter < _ticksPerFrame)
            return;

        _tickCounter = 0;
        _frameIndex = (_frameIndex + 1) % _frameCount;
    }
}
