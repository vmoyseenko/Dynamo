﻿using System.Windows.Media;

namespace Dynamo.Wpf.Interfaces
{
    public enum ResourceName
    {
        StartPageLogo,
        AboutBoxLogo
    }

    public interface IBrandingResourceProvider
    {
        ImageSource GetImageSource(ResourceName resourceName);
        string GetString(ResourceName resourceName);
    }
}
