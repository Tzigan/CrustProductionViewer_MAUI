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
        private DateTime lastUpdated;

        /// <summary>
        /// ������ ���� �������� � ����
        /// </summary>
        [ObservableProperty]
        private List<GameResource> resources = [];

        /// <summary>
        /// ������ ���� �������� � ����
        /// </summary>
        [ObservableProperty]
        private List<Building> buildings = [];

        /// <summary>
        /// ����� ������������ �������
        /// </summary>
        [ObservableProperty]
        private double totalEnergyConsumption;

        /// <summary>
        /// ����� ������������ �������
        /// </summary>
        [ObservableProperty]
        private double totalEnergyProduction;

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
        private int totalWorkers;

        /// <summary>
        /// ������������ ����������� ����������
        /// </summary>
        [ObservableProperty]
        private int maxWorkers;

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
