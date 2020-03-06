using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Repository.Interface
{
    public interface ISettingsRepository
    {
        Settings GetAllSettings();
        bool UpdateSettings(Settings settings);
    }
}
