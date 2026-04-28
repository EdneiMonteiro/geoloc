// Disclaimer
// Notice: Any sample scripts, code, or commands comes with the following notification.
//
// This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production
// environment. THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute
// the object code form of the Sample Code, provided that You agree: (i) to not use Our name, logo, or trademarks to
// market Your software product in which the Sample Code is embedded; (ii) to include a valid copyright notice on Your
// software product in which the Sample Code is embedded; and (iii) to indemnify, hold harmless, and defend Us and Our
// suppliers from and against any claims or lawsuits, including attorneys' fees, that arise or result from the use or
// distribution of the Sample Code.
//
// Please note: None of the conditions outlined in the disclaimer above will supersede the terms and conditions
// contained within the Customers Support Services Description.

const API_BASE_URL = __DEV__
  ? "http://192.168.3.214:7071/api"
  : "https://<YOUR_FUNCTION_APP>.azurewebsites.net/api";

export interface ValidationResult {
  isWithinRadius: boolean;
  distanceMeters: number;
  radiusMeters: number;
  registeredAddress: string;
  deviceCoordinates: { latitude: number; longitude: number };
  addressCoordinates: { latitude: number; longitude: number };
}

export interface ApiError {
  error: string;
}

export async function validateLocation(
  userId: string,
  latitude: number,
  longitude: number
): Promise<ValidationResult> {
  const response = await fetch(`${API_BASE_URL}/validate-location`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId, latitude, longitude }),
  });

  if (!response.ok) {
    const errorBody: ApiError = await response.json();
    throw new Error(errorBody.error || `HTTP ${response.status}`);
  }

  return response.json();
}
