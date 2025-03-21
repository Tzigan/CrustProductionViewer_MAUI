using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CrustProductionViewer_MAUI.Models
{
    /// <summary>
    /// ������������ �������� � ���� The Crust.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class Building : ObservableObject
    {
        /// <summary>
        /// ���������� ������������� ��������
        /// </summary>
        [ObservableProperty]
        private int id;

        /// <summary>
        /// �������� ��������
        /// </summary>
        [ObservableProperty]
        private string name = string.Empty; // ������������� ������ �������

        /// <summary>
        /// �������� ��������
        /// </summary>
        [ObservableProperty]
        private string description = string.Empty; // ������������� ������ �������

        /// <summary>
        /// ��� ��������
        /// </summary>
        [ObservableProperty]
        private BuildingType buildingType;

        /// <summary>
        /// ������� ��������
        /// </summary>
        [ObservableProperty]
        private int level;

        /// <summary>
        /// ������������� �������� (0.0 - 1.0)
        /// </summary>
        [ObservableProperty]
        private double efficiency;

        /// <summary>
        /// ������������ �������
        /// </summary>
        [ObservableProperty]
        private double energyConsumption;

        /// <summary>
        /// ���������� ������� ����
        /// </summary>
        [ObservableProperty]
        private int workersCapacity;

        /// <summary>
        /// ������� ���������� ����������
        /// </summary>
        [ObservableProperty]
        private int currentWorkers;

        /// <summary>
        /// ���� � ������ ��������
        /// </summary>
        [ObservableProperty]
        private string iconPath = string.Empty; // ������������� ������ �������

        /// <summary>
        /// ����� � ������ ��� ������ ����� ��������
        /// </summary>
        [ObservableProperty]
        private IntPtr memoryAddress;

        /// <summary>
        /// �������, ������������ ���� ���������
        /// </summary>
        [ObservableProperty]
        private List<ResourceProduction> producedResources = new();

        /// <summary>
        /// �������, ������������ ���� ���������
        /// </summary>
        [ObservableProperty]
        private List<ResourceConsumption> consumedResources = new();

        /// <summary>
        /// ������� �� ��������
        /// </summary>
        [ObservableProperty]
        private bool isActive;

        /// <summary>
        /// ������� ������������ �������� (���� ����)
        /// </summary>
        [ObservableProperty]
        private string inactiveReason = string.Empty; // ������������� ������ �������

        /// <summary>
        /// ������������ �������� (����������)
        /// </summary>
        [ObservableProperty]
        private BuildingLocation location = new(); // ������������� ����� �����������

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
    [SupportedOSPlatform("windows")]
    public partial class ResourceProduction : ObservableObject
    {
        /// <summary>
        /// ������������� �������
        /// </summary>
        [ObservableProperty]
        private int resourceId;

        /// <summary>
        /// ������� �������� ������������ (������ � ������)
        /// </summary>
        [ObservableProperty]
        private double baseProductionRate;

        /// <summary>
        /// ����������� �������� ������������ � ������ �������������
        /// </summary>
        [ObservableProperty]
        private double actualProductionRate;
    }

    /// <summary>
    /// ������������ ���������� � ����������� ������� ���������
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class ResourceConsumption : ObservableObject
    {
        /// <summary>
        /// ������������� �������
        /// </summary>
        [ObservableProperty]
        private int resourceId;

        /// <summary>
        /// ������� �������� ����������� (������ � ������)
        /// </summary>
        [ObservableProperty]
        private double baseConsumptionRate;

        /// <summary>
        /// ����������� �������� ����������� � ������ �������������
        /// </summary>
        [ObservableProperty]
        private double actualConsumptionRate;
    }

    /// <summary>
    /// ������������ �������� � ������� ����
    /// </summary>
    [SupportedOSPlatform("windows")]
    public partial class BuildingLocation : ObservableObject
    {
        /// <summary>
        /// ���������� X
        /// </summary>
        [ObservableProperty]
        private double x;

        /// <summary>
        /// ���������� Y
        /// </summary>
        [ObservableProperty]
        private double y;

        /// <summary>
        /// ���������� Z
        /// </summary>
        [ObservableProperty]
        private double z;
    }
}

