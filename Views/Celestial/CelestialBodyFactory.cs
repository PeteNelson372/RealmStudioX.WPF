using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Dialogs;
using System.IO;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace RealmStudioX.WPF.Views.Celestial;

/// <summary>
/// Creates and configures celestial bodies from the settings exposed by the
/// type-specific view models. Placement remains the responsibility of the
/// caller because it belongs to the CelestialBodyNode, not the body itself.
/// </summary>
internal static class CelestialBodyFactory
{
    private const string DefaultRingTextureUri =
        "pack://application:,,,/Assets/world_globe/saturnringcolor2.jpg";

    public static void Configure(
        CelestialBody body,
        CelestialBodyType bodyType,
        double rotationRate,
        WorldRotationDirection rotationDirection,
        BlackHoleViewModel blackHole,
        NebulaViewModel nebula,
        CoronaViewModel corona,
        SunViewModel sun,
        GasGiantViewModel gasGiant,
        TerrestrialPlanetViewModel terrestrialPlanet,
        Color? baseColor = null,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(blackHole);
        ArgumentNullException.ThrowIfNull(nebula);
        ArgumentNullException.ThrowIfNull(corona);
        ArgumentNullException.ThrowIfNull(sun);
        ArgumentNullException.ThrowIfNull(gasGiant);
        ArgumentNullException.ThrowIfNull(terrestrialPlanet);

        body.BodyType = bodyType;
        body.Name = bodyType.ToString();
        body.Description = description ?? $"Celestial body of type {bodyType}.";
        if (baseColor.HasValue)
            body.BaseColor = baseColor.Value;

        body.TextureSource = new CelestialTextureSource();

        // Rings are valid only for the two planet render configurations.
        if (bodyType is not CelestialBodyType.GasGiant
            and not CelestialBodyType.TerrestrialPlanet)
        {
            RemoveRingAttachment(body);
        }

        switch (bodyType)
        {
            case CelestialBodyType.Sun:
                ConfigureSun(body, rotationRate, rotationDirection, sun);
                break;

            case CelestialBodyType.Corona:
                ConfigureCorona(body, corona);
                break;

            case CelestialBodyType.BlackHole:
                ConfigureBlackHole(body, rotationRate, rotationDirection, blackHole);
                break;

            case CelestialBodyType.Nebula:
                ConfigureNebula(body, nebula);
                break;

            case CelestialBodyType.GasGiant:
                ConfigureGasGiant(body, rotationRate, rotationDirection, gasGiant);
                ConfigurePlanetaryRings(body, gasGiant.Rings);
                break;

            case CelestialBodyType.TerrestrialPlanet:
                ConfigureTerrestrialPlanet(body, rotationRate, rotationDirection, terrestrialPlanet);
                ConfigurePlanetaryRings(body, terrestrialPlanet.Rings);
                break;

            default:
                throw new NotSupportedException($"Celestial body type '{bodyType}' is not supported.");
        }
    }

    private static void ConfigureSun(
        CelestialBody body, double rotationRate, WorldRotationDirection direction, SunViewModel model)
    {
        body.RendererType = CelestialBodyRendererType.Sun;
        SetAnimation(body, CelestialBodyAnimationType.BasicRotation,
            new BasicRotationSettings { RotationRate = rotationRate, RotationDirection = direction });
        SetAnimation(body, CelestialBodyAnimationType.SolarActivity, new SunAnimationSettings
        {
            EnableProminences = model.EnableProminences,
            EnableFlares = model.EnableFlares,
            ActivityIntensity = model.ActivityIntensity,
            ActivityFrequency = model.ActivityFrequency,
            ActivityGeometryScale = model.ActivityGeometryScale,
            ActivityThicknessScale = model.ActivityThicknessScale,
            EnableCorona = model.EnableCorona,
            CoronaOpacity = model.CoronaOpacity,
            CoronaRotationSpeed = model.CoronaRotationSpeed
        });
        RemoveUnusedAnimations(body, CelestialBodyAnimationType.BasicRotation, CelestialBodyAnimationType.SolarActivity);
    }

    private static void ConfigureCorona(CelestialBody body, CoronaViewModel model)
    {
        body.RendererType = CelestialBodyRendererType.Corona;
        SetAnimation(body, CelestialBodyAnimationType.SolarActivity, new CoronaAnimationSettings
        {
            EnableProminences = model.EnableProminences,
            EnableFlares = model.EnableFlares,
            ActivityIntensity = model.ActivityIntensity,
            ActivityFrequency = model.ActivityFrequency,
            ActivityGeometryScale = model.ActivityGeometryScale,
            ActivityThicknessScale = model.ActivityThicknessScale
        });
        RemoveUnusedAnimations(body, CelestialBodyAnimationType.SolarActivity);
    }

    private static void ConfigureBlackHole(
        CelestialBody body, double rotationRate, WorldRotationDirection direction, BlackHoleViewModel model)
    {
        body.RendererType = CelestialBodyRendererType.BlackHole;
        SetAnimation(body, CelestialBodyAnimationType.BasicRotation,
            new BasicRotationSettings { RotationRate = rotationRate, RotationDirection = direction });
        SetAnimation(body, CelestialBodyAnimationType.BlackHoleActivity, new BlackHoleAnimationSettings
        {
            EnableJets = model.EnableJets,
            JetLength = model.JetLength,
            JetPulseRate = model.JetPulseRate,
            EnableJetParticles = model.EnableJetParticles,
            JetParticleSpeed = model.JetParticleSpeed,
            JetParticleEmissionRate = model.JetParticleEmissionRate,
            JetParticleLifetime = model.JetParticleLifetime,
            JetParticleDispersion = model.JetParticleDispersion,
            EnableDiskParticles = model.EnableDiskParticles,
            DiskParticleEmissionRate = model.DiskParticleEmissionRate,
            DiskParticleLifetime = model.DiskParticleLifetime,
            DiskParticleOutflowSpeed = model.DiskParticleOutflowSpeed
        });
        RemoveUnusedAnimations(body, CelestialBodyAnimationType.BasicRotation, CelestialBodyAnimationType.BlackHoleActivity);
    }

    private static void ConfigureNebula(CelestialBody body, NebulaViewModel model)
    {
        body.RendererType = CelestialBodyRendererType.Nebula;
        SetAnimation(body, CelestialBodyAnimationType.NebulaActivity, new NebulaAnimationSettings
        {
            LayerCount = model.LayerCount,
            Width = model.Width,
            Height = model.Height,
            Depth = model.Depth,
            Opacity = model.Opacity,
            Brightness = model.Brightness,
            DetailIntensity = model.DetailIntensity,
            RotationSpeed = model.RotationSpeed,
            DriftSpeed = model.DriftSpeed
        });
        RemoveUnusedAnimations(body, CelestialBodyAnimationType.NebulaActivity);
    }

    private static void ConfigureGasGiant(
        CelestialBody body, double rotationRate, WorldRotationDirection direction, GasGiantViewModel model)
    {
        body.RendererType = CelestialBodyRendererType.Planet;
        body.TextureSource = new CelestialTextureSource
        {
            SourceType = CelestialTextureSourceType.BitmapFile,
            FilePath = model.TexturePath
        };
        SetAnimation(body, CelestialBodyAnimationType.BasicRotation,
            new BasicRotationSettings { RotationRate = rotationRate, RotationDirection = direction });
        SetAnimation(body, CelestialBodyAnimationType.AtmosphereTurbulenceActivity, new GasGiantAnimationSettings
        {
            EnableAtmosphericTurbulence = model.EnableAtmosphericTurbulence,
            TurbulenceIntensity = model.TurbulenceIntensity
        });
        RemoveUnusedAnimations(body, CelestialBodyAnimationType.BasicRotation, CelestialBodyAnimationType.AtmosphereTurbulenceActivity);
    }

    private static void ConfigureTerrestrialPlanet(
        CelestialBody body, double rotationRate, WorldRotationDirection direction, TerrestrialPlanetViewModel model)
    {
        body.RendererType = CelestialBodyRendererType.Planet;

        // A RealmStudio map takes precedence over a bitmap texture.
        if (!string.IsNullOrWhiteSpace(model.MapIdentifier))
        {
            body.TextureSource = new CelestialTextureSource
            {
                SourceType = CelestialTextureSourceType.RealmMap,
                MapId = model.MapIdentifier
            };
        }
        else if (!string.IsNullOrWhiteSpace(model.TexturePath))
        {
            body.TextureSource = new CelestialTextureSource
            {
                SourceType = CelestialTextureSourceType.BitmapFile,
                FilePath = model.TexturePath
            };
        }

        SetAnimation(body, CelestialBodyAnimationType.BasicRotation,
            new BasicRotationSettings { RotationRate = rotationRate, RotationDirection = direction });
        RemoveUnusedAnimations(body, CelestialBodyAnimationType.BasicRotation);
    }

    private static void ConfigurePlanetaryRings(CelestialBody body, PlanetaryRingViewModel rings)
    {
        CelestialBodyAttachment? attachment = body.Attachments.FirstOrDefault(
            item => item.Type == CelestialBodyAttachmentType.PlanetaryRings);

        // Remove a previous attachment when rings are disabled or the selected
        // texture is unavailable. No ring geometry or animation should remain.
        if (!rings.Enabled || !CanLoadRingTexture(rings.Settings.TexturePath))
        {
            if (attachment is not null)
                body.Attachments.Remove(attachment);
            return;
        }

        // Reuse an existing attachment so its identity remains stable while
        // the Local Star's settings are being edited.
        if (attachment is null)
        {
            attachment = new CelestialBodyAttachment
            {
                Name = "Planetary Rings",
                Description = "A textured ring system surrounding the planet.",
                Type = CelestialBodyAttachmentType.PlanetaryRings,
                Enabled = true
            };
            body.Attachments.Add(attachment);
        }

        attachment.Enabled = true;
        attachment.AttachmentSettings = rings.Settings.Clone();

        SyncRingAnimation(attachment, rings.RotationAnimation);
        SyncRingAnimation(attachment, rings.TurbulenceAnimation);
    }

    private static void SyncRingAnimation(
        CelestialBodyAttachment attachment, CelestialBodyAnimation source)
    {
        CelestialBodyAnimation? existing = attachment.Animations.FirstOrDefault(
            item => item.AnimationType == source.AnimationType);
        CelestialBodyAnimation clone = CloneRingAnimation(source);

        // Preserve the renderer-facing identity while keeping settings objects
        // independent from the view model's animation instances.
        if (existing is not null)
            clone.Id = existing.Id;

        if (existing is null)
            attachment.Animations.Add(clone);
        else
        {
            int index = attachment.Animations.IndexOf(existing);
            attachment.Animations[index] = clone;
        }
    }

    private static bool CanLoadRingTexture(string? texturePath)
    {
        Uri uri;
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            uri = new Uri(DefaultRingTextureUri, UriKind.Absolute);
        }
        else if (Uri.TryCreate(texturePath, UriKind.Absolute, out Uri? suppliedUri)
            && suppliedUri.Scheme.Equals("pack", StringComparison.OrdinalIgnoreCase))
        {
            uri = suppliedUri;
        }
        else if (File.Exists(texturePath))
        {
            uri = new Uri(Path.GetFullPath(texturePath), UriKind.Absolute);
        }
        else
        {
            // A selected but missing custom texture is not silently replaced
            // by the built-in default.
            return false;
        }

        try
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            return bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0;
        }
        catch
        {
            return false;
        }
    }

    private static void RemoveRingAttachment(CelestialBody body)
    {
        for (int i = body.Attachments.Count - 1; i >= 0; i--)
        {
            if (body.Attachments[i].Type == CelestialBodyAttachmentType.PlanetaryRings)
                body.Attachments.RemoveAt(i);
        }
    }

    private static CelestialBodyAnimation CloneRingAnimation(CelestialBodyAnimation animation)
    {
        ICelestialBodyAnimationSettings settings = animation.Settings switch
        {
            BasicRotationSettings rotation => new BasicRotationSettings
            {
                RotationRate = rotation.RotationRate,
                RotationDirection = rotation.RotationDirection
            },
            RingTurbulenceSettings turbulence => new RingTurbulenceSettings
            {
                Intensity = turbulence.Intensity
            },
            _ => throw new NotSupportedException(
                $"Ring animation settings type '{animation.Settings?.GetType().Name}' is not supported.")
        };

        return new CelestialBodyAnimation
        {
            AnimationType = animation.AnimationType,
            Enabled = animation.Enabled,
            Settings = settings
        };
    }

    private static void SetAnimation(
        CelestialBody body,
        CelestialBodyAnimationType type,
        ICelestialBodyAnimationSettings settings)
    {
        CelestialBodyAnimation? animation = body.Animations.FirstOrDefault(a => a.AnimationType == type);
        if (animation is null)
        {
            animation = new CelestialBodyAnimation { AnimationType = type };
            body.Animations.Add(animation);
        }

        animation.Enabled = true;
        animation.Settings = settings;
    }

    private static void RemoveUnusedAnimations(
        CelestialBody body, params CelestialBodyAnimationType[] retainedTypes)
    {
        HashSet<CelestialBodyAnimationType> retained = [.. retainedTypes];
        for (int i = body.Animations.Count - 1; i >= 0; i--)
        {
            if (!retained.Contains(body.Animations[i].AnimationType))
                body.Animations.RemoveAt(i);
        }
    }
}
