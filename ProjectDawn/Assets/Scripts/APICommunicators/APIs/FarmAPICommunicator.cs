using System.Collections.Generic;
using System.Threading.Tasks;

public class FarmAPICommunicator : APIClientBase
{
    // -----------------------
    // GET ALL FARMS
    // -----------------------
    public Task<List<FarmInfoDTO>> GetAllFarms()
        => Get<List<FarmInfoDTO>>("/Farms/GetFarms");

    // -----------------------
    // GET FARM STATE
    // -----------------------
    public Task<FarmStateDM> GetFarmState(string farmId)
        => Get<FarmStateDM>($"/Farms/GetFarm/{farmId}");
}