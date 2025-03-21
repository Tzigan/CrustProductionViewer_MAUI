using System;
using System.Collections.Generic;
using System.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// ������������ ����� ���������� � ������������ � ����
    /// </summary>
    public partial class Production : ObservableObject
    {
        /// <summary>
        /// ����� ���������� ���������� ������
        /// </summary>
        [ObservableProperty]
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// ������ ���� �������� � ����
        /// </summary>
        [ObservableProperty]
        public List<GameResource> Resources { get; set; } = [];

        /// <summary>
        /// ������ ���� �������� � ����
        /// </summary>
        [ObservableProperty]
        public List<Building> Buildings { get; set; } = [];

        /// <summary>
        /// ����� ������������ �������
        /// </summary>
        [ObservableProperty]
        public double TotalEnergyConsumption { get; set; }

        /// <summary>
        /// ����� ������������ �������
        /// </summary>
        [ObservableProperty]
        public double TotalEnergyProduction { get; set; }

        /// <summary>
        /// ������ ������� (������������ - �����������)
        /// </summary>
        public double EnergyBalance => TotalEnergyProduction - TotalEnergyConsumption;

        /// <summary>
        /// ���������, ���������� �� ������� �������
        /// </summary>
        public bool HasEnergyDeficit => EnergyBalance < 0;

        /// <summary>
        /// ����� ���������� ����������
        /// </summary>
        [ObservableProperty]
        public int TotalWorkers { get; set; }

        /// <summary>
        /// ������������ ����������� ����������
        /// </summary>
        [ObservableProperty]
        public int MaxWorkers { get; set; }

        /// <summary>
        /// �������� ������ �� ��� ID
        /// </summary>
        /// <param name="resourceId">ID �������</param>
        /// <returns>������ ��� null, ���� �� ������</returns>
        public GameResource? GetResourceById(int resourceId)
        {
            return Resources.Find(r => r.Id == resourceId);
        }

        /// <summary>
        /// �������� �������� �� ��� ID
        /// </summary>
        /// <param name="buildingId">ID ��������</param>
        /// <returns>�������� ��� null, ���� �� �������</returns>
        public Building? GetBuildingById(int buildingId)
        {
            return Buildings.Find(b => b.Id == buildingId);
        }
    }
}
