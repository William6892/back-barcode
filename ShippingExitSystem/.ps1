$ApiBase = "http://localhost:5229/api"

Write-Host "1. Creando/Autenticando un Usuario Administrador..." -ForegroundColor Cyan

# Intentar login
$loginBody = @{
    Username = "admin_test"
    Password = "AdminPassword123!"
} | ConvertTo-Json

$loginResponse = $null
try {
    $loginResponse = Invoke-RestMethod -Uri "$ApiBase/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"
} catch {
    Write-Host "El usuario no existe, procediendo a registrarlo..."
    $registerBody = @{
        Username = "admin_test"
        Password = "AdminPassword123!"
        Email = "admin@test.com"
        Role = "Admin"
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$ApiBase/Auth/register" -Method Post -Body $registerBody -ContentType "application/json"
    $loginResponse = Invoke-RestMethod -Uri "$ApiBase/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"
}

$Token = $loginResponse.token
$Headers = @{
    Authorization = "Bearer $Token"
}

Write-Host "Token obtenido correctamente.`n" -ForegroundColor Green

Write-Host "2. Creando un nuevo Envío (Transport)..." -ForegroundColor Cyan
$randomEnvio = "ENV-$((Get-Random).ToString('0000'))"
$placa = "XYZ$((Get-Random -Minimum 100 -Maximum 999))"
$createShipmentBody = @{
    ShipmentNumber = $randomEnvio
    Carrier = "Test Express"
    DriverName = "Juan Perez"
    DriverDocument = "123456789"
    VehiclePlate = $placa
    VehicleModel = "Camioneta 2024"
    ScheduledDate = (Get-Date).AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
    ExpectedProducts = @(
        @{
            Name = "S25 Ultra"
            Model = "SM-S938B"
        }
    )
} | ConvertTo-Json

$shipmentResponse = Invoke-RestMethod -Uri "$ApiBase/Shipment" -Method Post -Body $createShipmentBody -ContentType "application/json" -Headers $Headers
$ShipmentId = $shipmentResponse.id
Write-Host "Envío creado con ID: $ShipmentId, Número: $randomEnvio, Placa: $placa`n" -ForegroundColor Green

Write-Host "3. Agregando otro producto al Envío..." -ForegroundColor Cyan
$addProductBody = @{
    Name = "Galaxy Watch 7"
    Model = "SM-R880"
} | ConvertTo-Json

$productResponse = Invoke-RestMethod -Uri "$ApiBase/Shipment/$ShipmentId/products" -Method Post -Body $addProductBody -ContentType "application/json" -Headers $Headers
Write-Host "Producto agregado con ID: $($productResponse.id), Nombre: $($productResponse.name)`n" -ForegroundColor Green

# Obtener los IDs de los productos esperados para escanear
$shipmentDetails = Invoke-RestMethod -Uri "$ApiBase/Shipment/$ShipmentId" -Method Get -Headers $Headers
$S25UltraId = ($shipmentDetails.products | Where-Object { $_.name -eq "S25 Ultra" }).expectedProductId
$Watch7Id = ($shipmentDetails.products | Where-Object { $_.name -eq "Galaxy Watch 7" }).expectedProductId

Write-Host "4. Escaneando 2 unidades de S25 Ultra y 1 de Watch 7..." -ForegroundColor Cyan
$barcode1 = "SN-S25-$((Get-Random).ToString('0000'))"
$barcode2 = "SN-S25-$((Get-Random).ToString('0000'))"
$barcode3 = "SN-W7-$((Get-Random).ToString('0000'))"

# Escanear barcode1
Invoke-RestMethod -Uri "$ApiBase/Shipment/$ShipmentId/scan" -Method Post -Body (@{ ExpectedProductId = $S25UltraId; Barcode = $barcode1 } | ConvertTo-Json) -ContentType "application/json" -Headers $Headers
Write-Host "  -> Escaneado: $barcode1"
# Escanear barcode2
Invoke-RestMethod -Uri "$ApiBase/Shipment/$ShipmentId/scan" -Method Post -Body (@{ ExpectedProductId = $S25UltraId; Barcode = $barcode2 } | ConvertTo-Json) -ContentType "application/json" -Headers $Headers
Write-Host "  -> Escaneado: $barcode2"
# Escanear barcode3
Invoke-RestMethod -Uri "$ApiBase/Shipment/$ShipmentId/scan" -Method Post -Body (@{ ExpectedProductId = $Watch7Id; Barcode = $barcode3 } | ConvertTo-Json) -ContentType "application/json" -Headers $Headers
Write-Host "  -> Escaneado: $barcode3`n"

Write-Host "5. Intentando escanear un duplicado ($barcode1)..." -ForegroundColor Cyan
try {
    Invoke-RestMethod -Uri "$ApiBase/Shipment/$ShipmentId/scan" -Method Post -Body (@{ ExpectedProductId = $S25UltraId; Barcode = $barcode1 } | ConvertTo-Json) -ContentType "application/json" -Headers $Headers
    Write-Host "Error: Se permitió escanear un duplicado." -ForegroundColor Red
} catch {
    Write-Host "Exitoso: El sistema bloqueó el escaneo duplicado correctamente." -ForegroundColor Green
    Write-Host $_.Exception.Response.StatusCode
}
Write-Host ""

Write-Host "6. Probando la nueva búsqueda de envíos por placa ($placa)..." -ForegroundColor Cyan
$searchByPlateResponse = Invoke-RestMethod -Uri "$ApiBase/Shipment/search-shipments?vehiclePlate=$placa" -Method Get -Headers $Headers
Write-Host "Encontrados: $($searchByPlateResponse.Count) envíos" -ForegroundColor Green
$firstShipmentFound = $searchByPlateResponse[0]
Write-Host "Número de Envío: $($firstShipmentFound.shipmentNumber)"
Write-Host "Productos despachados:"
foreach ($p in $firstShipmentFound.products) {
    Write-Host "  - $($p.name) (Modelo: $($p.model)): $($p.scannedCount) unidades"
    foreach ($item in $p.scannedItems) {
        Write-Host "      -> SN: $($item.barcode)"
    }
}
Write-Host ""

Write-Host "7. Completando el Envío..." -ForegroundColor Cyan
Invoke-RestMethod -Uri "$ApiBase/Shipment/$ShipmentId/complete" -Method Post -Headers $Headers
Write-Host "Envío completado exitosamente.`n" -ForegroundColor Green

Write-Host "8. Consultando el Dashboard Stats..." -ForegroundColor Cyan
$stats = Invoke-RestMethod -Uri "$ApiBase/Shipment/dashboard/stats" -Method Get -Headers $Headers
Write-Host "Envíos Activos: $($stats.activeShipmentsCount)"
Write-Host "Envíos Completados: $($stats.completedShipmentsCount)"
Write-Host "Total Envíos Hoy: $($stats.totalShipmentsToday)"
Write-Host "Total Escaneados Hoy: $($stats.totalProductsScannedToday)"

Write-Host "`n==== FLUJO PROBADO EXITOSAMENTE ====" -ForegroundColor Green
