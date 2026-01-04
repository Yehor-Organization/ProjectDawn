using System.Collections.Generic;
using System.Threading.Tasks;

public class FarmAPICommunicator : APIClientBase
{
    // -----------------------
    // GET ALL FARMS
    // -----------------------
    public Task<List<FarmInfoDTO>> GetAllFarms()
        => Get<List<FarmInfoDTO>>("/api/Farms/GetFarms");

    // -----------------------
    // GET FARM STATE
    // -----------------------
    public Task<FarmStateDM> GetFarmState(string farmId)
        => Get<FarmStateDM>($"/api/Farms/GetFarm/{farmId}");
}