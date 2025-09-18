using System.Collections.Generic;

[System.Serializable]
public class FarmInfoDto
{
    public int id;
    public string name;
    public string ownerName;
    public List<VisitorInfoDto> visitors;
}