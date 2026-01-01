using System.Collections.Generic;
using System.Threading.Tasks;

public class FarmAPICommunicator : APIClientBase
{
    // -----------------------
    // GET ALL FARMS
    // -----------------------
    public Task<List<FarmInfoDM>> GetAllFarms()
        => Get<List<FarmInfoDM>>("/api/Farms");

    // -----------------------
    // GET FARM STATE
    // -----------------------
    public Task<FarmStateDM> GetFarmState(string farmId)
        => Get<FarmStateDM>($"/api/Farms/{farmId}");
}