using DPCLibrary.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DPCLibraryTests
{
    [TestClass]
    [TestCategory("Administrator")]
    public class VPNProfileTests
    {

        /// <summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [DataTestMethod]
        [DataRow("This is a test")]
        [DataRow("<AlwaysOn>true</AlwaysOn><DeviceTunnel>true</DeviceTunnel><DnsSuffix>example.local</DnsSuffix><TrustedNetworkDetection>example.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.example.com;aovpn.example.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><Authentication><MachineMethod>Certificate</MachineMethod></Authentication><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize><Metric>1</Metric></Route><RegisterDNS>true</RegisterDNS>")] //null
        public void InvalidNotXMLProfile(string profile)
        {
            VPNProfile profile1 = new CSPProfile(profile, TestContext.TestName);
            TestContext.Write(profile1.LoadError);
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile1.LoadError));
        }

        [DataTestMethod]
        [DataRow("<Ele/>")]
        public void InvalidXMLProfile(string profile)
        {
            VPNProfile profile1 = new CSPProfile(profile, TestContext.TestName);
            VPNProfile profile2 = new CSPProfile(profile, TestContext.TestName);
            TestContext.Write(profile1.LoadError);
            TestContext.Write(profile2.LoadError);
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile1.LoadError));
            Assert.IsFalse(string.IsNullOrWhiteSpace(profile2.LoadError));
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(null)]
        [DataRow("<VPNProfile><AlwaysOn>true</AlwaysOn><DeviceTunnel>true</DeviceTunnel><DnsSuffix>example.local</DnsSuffix><TrustedNetworkDetection>example.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.example.com;aovpn.example.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><Authentication><MachineMethod>Certificate</MachineMethod></Authentication><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize><Metric>1</Metric></Route><RegisterDNS>true</RegisterDNS></VPNProfile>")] //Empty
        public void CompareSameProfile(string profile)
        {
            VPNProfile profile1 = new CSPProfile(profile, TestContext.TestName);
            VPNProfile profile2 = new CSPProfile(profile, TestContext.TestName);
            Assert.IsTrue(profile1 == profile2);
        }

		[DataTestMethod]
		[DataRow("<VPNProfile><NativeProfile><NativeProtocolType>Automatic</NativeProtocolType><Authentication><UserMethod>Mschapv2</UserMethod></Authentication></NativeProfile></VPNProfile>")]
		public void ValidDefaultProfile(string profile)
        {
			new CSPProfile(profile, TestContext.TestName);
        }

        [DataTestMethod]
        [DataRow(
"<VPNProfile><AlwaysOn>true</AlwaysOn><DeviceTunnel>true</DeviceTunnel><DnsSuffix>example.local</DnsSuffix><TrustedNetworkDetection>example.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.example.com;aovpn.example.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><Authentication><MachineMethod>Certificate</MachineMethod></Authentication><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize><Metric>1</Metric></Route><RegisterDNS>true</RegisterDNS></VPNProfile>",
@"
<VPNProfile>
	<AlwaysOn>true</AlwaysOn>
	<DeviceTunnel>true</DeviceTunnel>
	<DnsSuffix>example.local</DnsSuffix>
	<TrustedNetworkDetection>example.local</TrustedNetworkDetection>
	<NativeProfile>
		<Servers>aovpn.example.com;aovpn.example.com</Servers>
		<RoutingPolicyType>SplitTunnel</RoutingPolicyType>
		<NativeProtocolType>Ikev2</NativeProtocolType>
		<Authentication>
			<MachineMethod>Certificate</MachineMethod>
		</Authentication>
		<CryptographySuite>
			<AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants>
			<CipherTransformConstants>AES128</CipherTransformConstants>
			<PfsGroup>PFS2048</PfsGroup>
			<DHGroup>Group14</DHGroup>
			<IntegrityCheckMethod>SHA256</IntegrityCheckMethod>
			<EncryptionMethod>AES128</EncryptionMethod>
		</CryptographySuite>
		<DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute>
	</NativeProfile>
	<Route>
		<Address>10.0.0.0</Address>
		<PrefixSize>8</PrefixSize>
		<Metric>1</Metric>
	</Route>
	<RegisterDNS>true</RegisterDNS>
</VPNProfile>
")]
		[DataRow(
"<VPNProfile><RememberCredentials>true</RememberCredentials><AlwaysOn>true</AlwaysOn><DnsSuffix>example.local</DnsSuffix><TrustedNetworkDetection>example.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.example.com;aovpn.example.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><Authentication><UserMethod>Eap</UserMethod><MachineMethod>Eap</MachineMethod><Eap><Configuration><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>Leo-AONPS-01.example.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>13</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1\"><CredentialsSource><CertificateStore><SimpleCertSelection>true</SimpleCertSelection></CertificateStore></CredentialsSource><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>Leo-AONPS-01.example.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><DifferentUsername>false</DifferentUsername><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</AcceptServerName><TLSExtensions xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\"><FilteringInfo xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3\"><CAHashList Enabled=\"true\"><IssuerHash>5e e1 d7 2e ac 4a d3 23 57 c3 3e ff 1f 8c 7a 25 3c 1e 74 7a </IssuerHash></CAHashList></FilteringInfo></TLSExtensions></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></Configuration></Eap></Authentication><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize><Metric>1</Metric></Route></VPNProfile>",
"<VPNProfile><RememberCredentials>true</RememberCredentials><AlwaysOn>true</AlwaysOn><DnsSuffix>example.local</DnsSuffix><TrustedNetworkDetection>example.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.example.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><Authentication><UserMethod>Eap</UserMethod><Eap><Configuration><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>Leo-AONPS-01.example.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28</TrustedRootCA></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>13</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1\"><CredentialsSource><CertificateStore><SimpleCertSelection>true</SimpleCertSelection></CertificateStore></CredentialsSource><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>Leo-AONPS-01.example.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28</TrustedRootCA></ServerValidation><DifferentUsername>false</DifferentUsername><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</AcceptServerName><TLSExtensions xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\"><FilteringInfo xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3\"><CAHashList Enabled=\"true\"><IssuerHash>5e e1 d7 2e ac 4a d3 23 57 c3 3e ff 1f 8c 7a 25 3c 1e 74 7a</IssuerHash></CAHashList></FilteringInfo></TLSExtensions></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></Configuration></Eap></Authentication></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize></Route></VPNProfile>")]
		public void Compare2SameProfiles(string profileStr, string profileStr2)
        {
            VPNProfile profile1 = new CSPProfile(profileStr, TestContext.TestName);
            VPNProfile profile2 = new CSPProfile(profileStr2, TestContext.TestName);
            Assert.IsTrue(profile1 == profile2);
        }

        [DataTestMethod]
        [DataRow(
"<VPNProfile><RememberCredentials>true</RememberCredentials><AlwaysOn>true</AlwaysOn><DnsSuffix>example.local</DnsSuffix><TrustedNetworkDetection>example.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.example.com;aovpn.example.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><Authentication><UserMethod>Eap</UserMethod><MachineMethod>Eap</MachineMethod><Eap><Configuration><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>Leo-AONPS-01.example.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>13</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1\"><CredentialsSource><CertificateStore><SimpleCertSelection>true</SimpleCertSelection></CertificateStore></CredentialsSource><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>Leo-AONPS-01.example.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><DifferentUsername>false</DifferentUsername><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</AcceptServerName><TLSExtensions xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\"><FilteringInfo xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3\"><CAHashList Enabled=\"true\"><IssuerHash>5e e1 d7 2e ac 4a d3 23 57 c3 3e ff 1f 8c 7a 25 3c 1e 74 7a </IssuerHash></CAHashList></FilteringInfo></TLSExtensions></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></Configuration></Eap></Authentication><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize><Metric>1</Metric></Route></VPNProfile>",
@"
<VPNProfile>
 <RememberCredentials>true</RememberCredentials>
 <AlwaysOn>true</AlwaysOn>
 <DnsSuffix>example.local</DnsSuffix>
 <TrustedNetworkDetection>example.local</TrustedNetworkDetection>
 <NativeProfile>
  <Servers>aovpn.example.com</Servers>
  <RoutingPolicyType>SplitTunnel</RoutingPolicyType>
  <NativeProtocolType>Ikev2</NativeProtocolType>
  <DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute>
  <CryptographySuite>
   <AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants>
   <CipherTransformConstants>AES128</CipherTransformConstants>
   <PfsGroup>PFS2048</PfsGroup>
   <DHGroup>Group14</DHGroup>
   <IntegrityCheckMethod>SHA256</IntegrityCheckMethod>
   <EncryptionMethod>AES128</EncryptionMethod>
  </CryptographySuite>
  <Authentication>
   <UserMethod>Eap</UserMethod>
   <Eap>
    <Configuration>
     <EapHostConfig xmlns=""http://www.microsoft.com/provisioning/EapHostConfig"">
      <EapMethod>
       <Type xmlns=""http://www.microsoft.com/provisioning/EapCommon"">25</Type>
       <VendorId xmlns=""http://www.microsoft.com/provisioning/EapCommon"">0</VendorId>
       <VendorType xmlns=""http://www.microsoft.com/provisioning/EapCommon"">0</VendorType>
       <AuthorId xmlns=""http://www.microsoft.com/provisioning/EapCommon"">0</AuthorId>
      </EapMethod>
      <Config xmlns=""http://www.microsoft.com/provisioning/EapHostConfig"">
       <Eap xmlns=""http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1"">
        <Type>25</Type>
        <EapType xmlns=""http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1"">
         <ServerValidation>
          <DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation>
          <ServerNames>Leo-AONPS-01.example.local</ServerNames>
          <TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28</TrustedRootCA>
         </ServerValidation>
         <FastReconnect>true</FastReconnect>
         <InnerEapOptional>false</InnerEapOptional>
         <Eap xmlns=""http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1"">
          <Type>13</Type>
          <EapType xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1"">
           <CredentialsSource>
            <CertificateStore>
             <SimpleCertSelection>true</SimpleCertSelection>
            </CertificateStore>
           </CredentialsSource>
           <ServerValidation>
            <DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation>
            <ServerNames>Leo-AONPS-01.example.local</ServerNames>
            <TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28</TrustedRootCA>
           </ServerValidation>
           <DifferentUsername>false</DifferentUsername>
           <PerformServerValidation xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2"">true</PerformServerValidation>
           <AcceptServerName xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2"">true</AcceptServerName>
           <TLSExtensions xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2"">
            <FilteringInfo xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3"">
             <CAHashList Enabled=""true"">
              <IssuerHash>5e e1 d7 2e ac 4a d3 23 57 c3 3e ff 1f 8c 7a 25 3c 1e 74 7a</IssuerHash>
             </CAHashList>
            </FilteringInfo>
           </TLSExtensions>
          </EapType>
         </Eap>
         <EnableQuarantineChecks>false</EnableQuarantineChecks>
         <RequireCryptoBinding>false</RequireCryptoBinding>
         <PeapExtensions>
          <PerformServerValidation xmlns=""http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2"">true</PerformServerValidation>
          <AcceptServerName xmlns=""http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2"">true</AcceptServerName>
         </PeapExtensions>
        </EapType>
       </Eap>
      </Config>
     </EapHostConfig>
    </Configuration>
   </Eap>
  </Authentication>
 </NativeProfile>
 <Route>
  <Address>172.0.0.0</Address>
  <PrefixSize>8</PrefixSize>
 </Route>
</VPNProfile>
")]
        public void Compare2DiffProfiles(string profileStr, string profileStr2)
        {
            VPNProfile profile1 = new CSPProfile(profileStr, TestContext.TestName);
            VPNProfile profile2 = new CSPProfile(profileStr2, TestContext.TestName);
            Assert.IsFalse(profile1 == profile2);
        }

        [DataTestMethod]
        [DataRow(
"<VPNProfile><RememberCredentials>true</RememberCredentials><AlwaysOn>true</AlwaysOn><DnsSuffix>example.local</DnsSuffix><TrustedNetworkDetection>example.local</TrustedNetworkDetection><NativeProfile><Servers>aovpn.example.com;aovpn.example.com</Servers><RoutingPolicyType>SplitTunnel</RoutingPolicyType><NativeProtocolType>Ikev2</NativeProtocolType><Authentication><UserMethod>Eap</UserMethod><MachineMethod>Eap</MachineMethod><Eap><Configuration><EapHostConfig xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><EapMethod><Type xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">25</Type><VendorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorId><VendorType xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</VendorType><AuthorId xmlns=\"http://www.microsoft.com/provisioning/EapCommon\">0</AuthorId></EapMethod><Config xmlns=\"http://www.microsoft.com/provisioning/EapHostConfig\"><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>25</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1\"><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>Leo-AONPS-01.example.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><FastReconnect>true</FastReconnect><InnerEapOptional>false</InnerEapOptional><Eap xmlns=\"http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1\"><Type>13</Type><EapType xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1\"><CredentialsSource><CertificateStore><SimpleCertSelection>true</SimpleCertSelection></CertificateStore></CredentialsSource><ServerValidation><DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation><ServerNames>Leo-AONPS-01.example.local</ServerNames><TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28 </TrustedRootCA></ServerValidation><DifferentUsername>false</DifferentUsername><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\">true</AcceptServerName><TLSExtensions xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2\"><FilteringInfo xmlns=\"http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3\"><CAHashList Enabled=\"true\"><IssuerHash>5e e1 d7 2e ac 4a d3 23 57 c3 3e ff 1f 8c 7a 25 3c 1e 74 7a </IssuerHash></CAHashList></FilteringInfo></TLSExtensions></EapType></Eap><EnableQuarantineChecks>false</EnableQuarantineChecks><RequireCryptoBinding>false</RequireCryptoBinding><PeapExtensions><PerformServerValidation xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</PerformServerValidation><AcceptServerName xmlns=\"http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2\">true</AcceptServerName></PeapExtensions></EapType></Eap></Config></EapHostConfig></Configuration></Eap></Authentication><CryptographySuite><AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants><CipherTransformConstants>AES128</CipherTransformConstants><PfsGroup>PFS2048</PfsGroup><DHGroup>Group14</DHGroup><IntegrityCheckMethod>SHA256</IntegrityCheckMethod><EncryptionMethod>AES128</EncryptionMethod></CryptographySuite><DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute></NativeProfile><Route><Address>10.0.0.0</Address><PrefixSize>8</PrefixSize><Metric>1</Metric></Route></VPNProfile>",
@"
<VPNProfile>
 <RememberCredentials>true</RememberCredentials>
 <AlwaysOn>true</AlwaysOn>
 <DnsSuffix>example.local</DnsSuffix>
 <TrustedNetworkDetection>example.local</TrustedNetworkDetection>
 <NativeProfile>
  <Servers>aovpn.example.com</Servers>
  <RoutingPolicyType>SplitTunnel</RoutingPolicyType>
  <NativeProtocolType>Ikev2</NativeProtocolType>
  <DisableClassBasedDefaultRoute>true</DisableClassBasedDefaultRoute>
  <CryptographySuite>
   <AuthenticationTransformConstants>SHA256128</AuthenticationTransformConstants>
   <CipherTransformConstants>AES128</CipherTransformConstants>
   <PfsGroup>PFS2048</PfsGroup>
   <DHGroup>Group14</DHGroup>
   <IntegrityCheckMethod>SHA256</IntegrityCheckMethod>
   <EncryptionMethod>AES128</EncryptionMethod>
  </CryptographySuite>
  <Authentication>
   <UserMethod>Eap</UserMethod>
   <Eap>
    <Configuration>
     <EapHostConfig xmlns=""http://www.microsoft.com/provisioning/EapHostConfig"">
      <EapMethod>
       <Type xmlns=""http://www.microsoft.com/provisioning/EapCommon"">25</Type>
       <VendorId xmlns=""http://www.microsoft.com/provisioning/EapCommon"">0</VendorId>
       <VendorType xmlns=""http://www.microsoft.com/provisioning/EapCommon"">0</VendorType>
       <AuthorId xmlns=""http://www.microsoft.com/provisioning/EapCommon"">0</AuthorId>
      </EapMethod>
      <Config xmlns=""http://www.microsoft.com/provisioning/EapHostConfig"">
       <Eap xmlns=""http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1"">
        <Type>25</Type>
        <EapType xmlns=""http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV1"">
         <ServerValidation>
          <DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation>
          <ServerNames>Leo-AONPS-01.example.local</ServerNames>
          <TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28</TrustedRootCA>
         </ServerValidation>
         <FastReconnect>true</FastReconnect>
         <InnerEapOptional>false</InnerEapOptional>
         <Eap xmlns=""http://www.microsoft.com/provisioning/BaseEapConnectionPropertiesV1"">
          <Type>13</Type>
          <EapType xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV1"">
           <CredentialsSource>
            <CertificateStore>
             <SimpleCertSelection>true</SimpleCertSelection>
            </CertificateStore>
           </CredentialsSource>
           <ServerValidation>
            <DisableUserPromptForServerValidation>true</DisableUserPromptForServerValidation>
            <ServerNames>Leo-AONPS-01.example.local</ServerNames>
            <TrustedRootCA>05 49 d9 e2 d6 8c 0e 18 48 9f ad 29 8c 03 62 62 1d 33 42 28</TrustedRootCA>
           </ServerValidation>
           <DifferentUsername>false</DifferentUsername>
           <PerformServerValidation xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2"">true</PerformServerValidation>
           <AcceptServerName xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2"">true</AcceptServerName>
           <TLSExtensions xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV2"">
            <FilteringInfo xmlns=""http://www.microsoft.com/provisioning/EapTlsConnectionPropertiesV3"">
             <CAHashList Enabled=""true"">
              <IssuerHash>5e e1 d7 2e ac 4a d3 23 57 c3 3e ff 1f 8c 7a 25 3c 1e 74 7a</IssuerHash>
             </CAHashList>
            </FilteringInfo>
           </TLSExtensions>
          </EapType>
         </Eap>
         <EnableQuarantineChecks>false</EnableQuarantineChecks>
         <RequireCryptoBinding>false</RequireCryptoBinding>
         <PeapExtensions>
          <PerformServerValidation xmlns=""http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2"">true</PerformServerValidation>
          <AcceptServerName xmlns=""http://www.microsoft.com/provisioning/MsPeapConnectionPropertiesV2"">true</AcceptServerName>
         </PeapExtensions>
        </EapType>
       </Eap>
      </Config>
     </EapHostConfig>
    </Configuration>
   </Eap>
  </Authentication>
 </NativeProfile>
 <Route>
 </Route>
</VPNProfile>
")]
        public void Compare2DiffProfilesInvalidRoutes(string profileStr, string profileStr2)
        {
            VPNProfile profile1 = new CSPProfile(profileStr, TestContext.TestName);
            VPNProfile profile2 = new CSPProfile(profileStr2, TestContext.TestName);
            Assert.IsFalse(profile1 == profile2);
        }
    }
}
