// TimerOne library: https://code.google.com/p/arduino-timerone/
#include <TimerOne.h> // KURTZ: remove?


#include <SPI.h>
#include <BLEPeripheral.h>

// define pins (varies per shield/board) See http://redbearlab.com/blendmicro#technicaldetails
#define BLE_REQ   10 // REQN
#define BLE_RDY   2  // RDYN
#define BLE_RST   9  // not really sure what this is. 

#define LED_PIN   13

// service > characteristic > descriptor

BLEPeripheral blePeripheral = BLEPeripheral(BLE_REQ, BLE_RDY, BLE_RST);

BLEService cscService = BLEService("1816"); // https://developer.bluetooth.org/gatt/services/Pages/ServiceViewer.aspx?u=org.bluetooth.service.cycling_speed_and_cadence.xml
BLEFloatCharacteristic cscMeasurementCharacteristic = BLEFloatCharacteristic("2A5B", BLERead | BLENotify); // Flags and stuff. https://developer.bluetooth.org/gatt/characteristics/Pages/CharacteristicViewer.aspx?u=org.bluetooth.characteristic.csc_measurement.xml
BLEFloatCharacteristic cscFeatureCharacteristic = BLEFloatCharacteristic("2A5C", BLERead | BLENotify);  // Describes supported features. https://developer.bluetooth.org/gatt/characteristics/Pages/CharacteristicViewer.aspx?u=org.bluetooth.characteristic.csc_feature.xml

// Other CSC characteristics are considered optional for this application and are thus not being developed. 

//BLEDescriptor tempDescriptor = BLEDescriptor("2901", "Temp Celsius"); // sample Descriptor declaration


volatile bool readFromSensor = false;

float lastTempReading;
float lastHumidityReading;

void setup() {
  Serial.begin(115200);
#if defined (__AVR_ATmega32U4__)
  //Wait until the serial port is available (useful only for the Leonardo)
  //As the Leonardo board is not reseted every time you open the Serial Monitor
  while(!Serial) {}
  delay(5000);  //5 seconds delay for enabling to see the start up comments on the serial board
#endif

  blePeripheral.setLocalName("Cyclometer");
  
  blePeripheral.setAdvertisedServiceUuid(cscService.uuid());
  blePeripheral.addAttribute(cscService);
  blePeripheral.addAttribute(cscMeasurementCharacteristic);
  blePeripheral.addAttribute(cscFeatureCharacteristic);
 
  // the above section would be repeated as needed for other services. 
  
//  blePeripheral.addAttribute(humidityDescriptor);

  blePeripheral.setEventHandler(BLEConnected, blePeripheralConnectHandler);
  blePeripheral.setEventHandler(BLEDisconnected, blePeripheralDisconnectHandler);

  blePeripheral.begin();
  
//  Timer1.initialize(2 * 1000000); // in milliseconds
//  Timer1.attachInterrupt(timerHandler);
  
  Serial.println(F("BLE Cycling Speed and Cadence Sensor"));
}

void loop() {
  blePeripheral.poll();
  
  if (readFromSensor) {
    setTempCharacteristicValue();
    setHumidityCharacteristicValue();
    readFromSensor = false;
  }
}

void timerHandler() {
  readFromSensor = true;
}

//void setTempCharacteristicValue() {
////  float reading = dht.readTemperature();
//    float reading = random(100);
//  
//  if (!isnan(reading) && significantChange(lastTempReading, reading, 0.5)) {
//    tempCharacteristic.setValue(reading);
//    
//    Serial.print(F("Temperature: ")); Serial.print(reading); Serial.println(F("C"));
//    
//    lastTempReading = reading;
//  }
//}

//void setHumidityCharacteristicValue() {
////  float reading = dht.readHumidity();
//  float reading = random(100);
//
//  if (!isnan(reading) && significantChange(lastHumidityReading, reading, 1.0)) {
//    humidityCharacteristic.setValue(reading);
//    
//    Serial.print(F("Humidity: ")); Serial.print(reading); Serial.println(F("%"));
//    
//    lastHumidityReading = reading;
//  }
//}

//boolean significantChange(float val1, float val2, float threshold) {
//  return (abs(val1 - val2) >= threshold);
//}

void blePeripheralConnectHandler(BLECentral& central) {
  Serial.print(F("Connected event, central: "));
  Serial.println(central.address());
}

void blePeripheralDisconnectHandler(BLECentral& central) {
  Serial.print(F("Disconnected event, central: "));
  Serial.println(central.address());
}

