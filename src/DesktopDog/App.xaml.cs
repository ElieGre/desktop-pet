using System.Windows;
using DesktopPet;
using DesktopPet.Pet;
using Application = System.Windows.Application;

namespace DesktopDog;

/// <summary>
/// The dog exe: nothing but a PetWindow bound to the dog profile. All behaviour lives in
/// DesktopPet.Core, shared with the crocodile — the two can run side by side.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        new PetWindow(PetProfile.Dog).Show();
    }
}
