using System.Collections.Generic;

[System.Serializable]
public class FarmInfoDM
{
    public int id;
    public string name;
    public string ownerName;
    public List<VisitorInfoDC> visitors;
}