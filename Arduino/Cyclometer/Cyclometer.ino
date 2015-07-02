// TimerOne library: https://code.google.com/p/arduino-timerone/
//#include <TimerOne.h> // KURTZ: remove?


#include <SPI.h>
#include <BLEPeripheral.h>

// define pins (varies per shield/board) See http://redbearlab.com/blendmicro#technicaldetails
#define BLE_REQ   10 // REQN
#define BLE_RDY   2  // RDYN
#define BLE_RST   9  // not really sure what this is. 

#define SENSOR_PIN    3  // the pin to which the magnetic sensor is attached
#define SENSOR_INT    1  // the interrupt number for the sensor. 

#define LED_PIN   13

// peripheral > service > characteristic > descriptor

// create peripheral instance, see pinouts above
BLEPeripheral blePeripheral = BLEPeripheral(BLE_REQ, BLE_RDY, BLE_RST);

// create service
BLEService cscService = BLEService("1816"); // https://developer.bluetooth.org/gatt/services/Pages/ServiceViewer.aspx?u=org.bluetooth.service.cycling_speed_and_cadence.xml

// create characteristic
BLECharacteristic cscMeasurementCharacteristic = BLEFloatCharacteristic("2A5B", BLERead | BLENotify); // Flags and stuff. https://developer.bluetooth.org/gatt/characteristics/Pages/CharacteristicViewer.aspx?u=org.bluetooth.characteristic.csc_measurement.xml
BLECharacteristic cscFeatureCharacteristic = BLEFloatCharacteristic("2A5C", BLERead | BLENotify);  // Describes supported features. https://developer.bluetooth.org/gatt/characteristics/Pages/CharacteristicViewer.aspx?u=org.bluetooth.characteristic.csc_feature.xml


// Other CSC characteristics are considered optional for this application and are thus not being developed. 

//BLEDescriptor tempDescriptor = BLEDescriptor("2901", "Temp Celsius"); // sample Descriptor declaration


volatile bool newSensorData = false;

byte flags = B01; // has wheel data; doesn't have crank data. 
volatile unsigned long cumWheelRevs = 0; // unsigned long is equivalent to uint32
volatile unsigned int lastWheelEventTime = 0; // unsigned long is equivalent to uint16


void setup() {
  Serial.begin(115200);
#if defined (__AVR_ATmega32U4__)
  //Wait until the serial port is available (useful only for the Leonardo)
  //As the Leonardo board is not reseted every time you open the Serial Monitor
  while(!Serial) {}
  delay(5000);  //5 seconds delay for enabling to see the start up comments on the serial board
#endif

  /*----- start BLE Utility --------*/
  
  // set advertised local name and service UUID
  blePeripheral.setLocalName("Cyclometer");
  blePeripheral.setAdvertisedServiceUuid(cscService.uuid());

  // add service and characteristic
  blePeripheral.addAttribute(cscService);
  blePeripheral.addAttribute(cscMeasurementCharacteristic);
  blePeripheral.addAttribute(cscFeatureCharacteristic);
 
  // the above section would be repeated as needed for other services. 

  // add event handlers for connections being created and lost. 
  blePeripheral.setEventHandler(BLEConnected, blePeripheralConnectHandler);
  blePeripheral.setEventHandler(BLEDisconnected, blePeripheralDisconnectHandler);

  // begin initialization
  blePeripheral.begin();

  /*----- end BLE Utility --------*/
  

  attachInterrupt(SENSOR_INT, incrementWheel, RISING);
  
  Serial.println(F("BLE Cycling Speed and Cadence Sensor"));
}

void loop() {
  blePeripheral.poll();
  
  if (newSensorData) {
    setMeasurementCharacteristicValue();
    newSensorData = false;
  }
}


void incrementWheel() {
  cumWheelRevs++; 
  lastWheelEventTime = millis() * 1.024; // concerting from millis to 1/1024 sec, as required by BLE spec. 
  newSensorData = true;
}


void setMeasurementCharacteristicValue() {
  cscMeasurementCharacteristic.setValue(cumWheelRevs); // not the current data format. 
  
  Serial.print(F("Wheel Revs: ")); Serial.println(cumWheelRevs); // should take format defined by https://developer.bluetooth.org/gatt/characteristics/Pages/CharacteristicViewer.aspx?u=org.bluetooth.characteristic.csc_measurement.xml
}


void blePeripheralConnectHandler(BLECentral& central) {
  Serial.print(F("Connected event, central: "));
  Serial.println(central.address());
}


void blePeripheralDisconnectHandler(BLECentral& central) {
  Serial.print(F("Disconnected event, central: "));
  Serial.println(central.address());
}

