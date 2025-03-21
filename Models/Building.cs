using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// ������������ �������� � ���� The Crust.
    /// </summary>
    public partial class Building : ObservableObject
    {
        /// <summary>
        /// ���������� ������������� ��������
        /// </summary>
        [ObservableProperty]
        public int Id { get; set; }

        /// <summary>
        /// �������� ��������
        /// </summary>
        [ObservableProperty]
        public string Name { get; set; } = string.Empty; // ������������� ������ �������

        /// <summary>
        /// �������� ��������
        /// </summary>
        [ObservableProperty]
        public string Description { get; set; } = string.Empty; // ������������� ������ �������

        /// <summary>
        /// ��� ��������
        /// </summary>
        [ObservableProperty]
        public BuildingType BuildingType { get; set; }

        /// <summary>
        /// ������� ��������
        /// </summary>
        [ObservableProperty]
        public int Level { get; set; }

        /// <summary>
        /// ������������� �������� (0.0 - 1.0)
        /// </summary>
        [ObservableProperty]
        public double Efficiency { get; set; }

        /// <summary>
        /// ������������ �������
        /// </summary>
        [ObservableProperty]
        public double EnergyConsumption { get; set; }

        /// <summary>
        /// ���������� ������� ����
        /// </summary>
        [ObservableProperty]
        public int WorkersCapacity { get; set; }

        /// <summary>
        /// ������� ���������� ����������
        /// </summary>
        [ObservableProperty]
        public int CurrentWorkers { get; set; }

        /// <summary>
        /// ���� � ������ ��������
        /// </summary>
        [ObservableProperty]
        public string IconPath { get; set; } = string.Empty; // ������������� ������ �������

        /// <summary>
        /// ����� � ������ ��� ������ ����� ��������
        /// </summary>
        [ObservableProperty]
        public IntPtr MemoryAddress { get; set; }

        /// <summary>
        /// �������, ������������ ���� ���������
        /// </summary>
        [ObservableProperty]
        public List<ResourceProduction> ProducedResources { get; set; } = new();

        /// <summary>
        /// �������, ������������ ���� ���������
        /// </summary>
        [ObservableProperty]
        public List<ResourceConsumption> ConsumedResources { get; set; } = new();

        /// <summary>
        /// ������� �� ��������
        /// </summary>
        [ObservableProperty]
        public bool IsActive { get; set; }

        /// <summary>
        /// ������� ������������ �������� (���� ����)
        /// </summary>
        [ObservableProperty]
        public string InactiveReason { get; set; } = string.Empty; // ������������� ������ �������

        /// <summary>
        /// ������������ �������� (����������)
        /// </summary>
        [ObservableProperty]
        public BuildingLocation Location { get; set; } = new(); // ������������� ����� �����������

        /// <summary>
        /// ����������� ������������� � ������ ���� ��������
        /// </summary>
        public double ActualEfficiency =>
            IsActive ? Efficiency * (WorkersCapacity > 0 ? (double)CurrentWorkers / WorkersCapacity : 1.0) : 0.0;
    }

    /// <summary>
    /// ���� �������� � ����
    /// </summary>
    public enum BuildingType
    {
        /// <summary>
        /// ������ ��������
        /// </summary>
        Extraction,

        /// <summary>
        /// ������������
        /// </summary>
        Production,

        /// <summary>
        /// �������������� ��������
        /// </summary>
        Power,

        /// <summary>
        /// ������
        /// </summary>
        Storage,

        /// <summary>
        /// �����
        /// </summary>
        Housing,

        /// <summary>
        /// ��������� � ���������
        /// </summary>
        Transport,

        /// <summary>
        /// ����������� ��������
        /// </summary>
        Special
    }

    /// <summary>
    /// ������������ ���������� � ������������ ������� ���������
    /// </summary>
    public partial class ResourceProduction : ObservableObject
    {
        /// <summary>
        /// ������������� �������
        /// </summary>
        [ObservableProperty]
        public int ResourceId { get; set; }

        /// <summary>
        /// ������� �������� ������������ (������ � ������)
        /// </summary>
        [ObservableProperty]
        public double BaseProductionRate { get; set; }

        /// <summary>
        /// ����������� �������� ������������ � ������ �������������
        /// </summary>
        [ObservableProperty]
        public double ActualProductionRate { get; set; }
    }

    /// <summary>
    /// ������������ ���������� � ����������� ������� ���������
    /// </summary>
    public partial class ResourceConsumption : ObservableObject
    {
        /// <summary>
        /// ������������� �������
        /// </summary>
        [ObservableProperty]
        public int ResourceId { get; set; }

        /// <summary>
        /// ������� �������� ����������� (������ � ������)
        /// </summary>
        [ObservableProperty]
        public double BaseConsumptionRate { get; set; }

        /// <summary>
        /// ����������� �������� ����������� � ������ �������������
        /// </summary>
        [ObservableProperty]
        public double ActualConsumptionRate { get; set; }
    }

    /// <summary>
    /// ������������ �������� � ������� ����
    /// </summary>
    public partial class BuildingLocation : ObservableObject
    {
        /// <summary>
        /// ���������� X
        /// </summary>
        [ObservableProperty]
        public double X { get; set; }

        /// <summary>
        /// ���������� Y
        /// </summary>
        [ObservableProperty]
        public double Y { get; set; }

        /// <summary>
        /// ���������� Z
        /// </summary>
        [ObservableProperty]
        public double Z { get; set; }
    }
}
